using System.Collections.Concurrent;
using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Infrastructure.Live;

public sealed class LiveMastodonTimelineClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider)
    : MastodonCompatibleTimelineClient(
        httpClientFactory,
        configProvider,
        FediverseSourceType.Mastodon,
        static config => config.Mastodon)
{
}

public sealed class LivePixelfedTimelineClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider)
    : MastodonCompatibleTimelineClient(
        httpClientFactory,
        configProvider,
        FediverseSourceType.Pixelfed,
        static config => config.Pixelfed)
{
}

public abstract class MastodonCompatibleTimelineClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider,
    FediverseSourceType sourceType,
    Func<FediverseInstancesConfig, InstanceConfig> configSelector) : IFediverseSourceClient
{
    private readonly ConcurrentDictionary<string, string> _maxIdByPage = new(StringComparer.Ordinal);

    public FediverseSourceType SourceType { get; } = sourceType;

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AuthStatus.Demo(SourceType, SourceType.ToString()));

    public Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new SourceCapabilitySet
        {
            SourceType = SourceType,
            SupportsPosting = false,
            SupportsHashtagFollowing = false,
            SupportsMediaUpload = false
        });

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var config = await LoadConfigAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var baseUri = LiveFeedJson.BaseUri(config.BaseUrl);
        var key = $"public:{baseUri.Host}";
        var uri = BuildUri(baseUri, "api/v1/timelines/public", request, key);
        var items = await FetchStatusesAsync(baseUri, uri, request, key, cancellationToken)
            .ConfigureAwait(false);

        return items.Count > 0
            ? items
            : await FetchFallbackHashtagsAsync(baseUri, request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        if (normalized.Length == 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var config = await LoadConfigAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var tag = Uri.EscapeDataString(normalized.TrimStart('#'));
        var baseUri = LiveFeedJson.BaseUri(config.BaseUrl);
        var key = $"tag:{baseUri.Host}:{normalized}";
        var uri = BuildUri(baseUri, $"api/v1/timelines/tag/{tag}", request, key);
        return await FetchStatusesAsync(baseUri, uri, request, key, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<UnifiedTimelineItem>> FetchFallbackHashtagsAsync(
        Uri baseUri,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        string[] fallbackTags = ["fediverse", "mastodon", "opensource"];
        var tasks = fallbackTags.Select(tag =>
        {
            var normalized = "#" + tag;
            var key = $"fallback:{baseUri.Host}:{normalized}";
            var uri = BuildUri(baseUri, $"api/v1/timelines/tag/{tag}", request, key);
            return FetchStatusesAsync(baseUri, uri, request, key, cancellationToken);
        });
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results
            .SelectMany(static result => result)
            .OrderByDescending(static item => item.PublishedAt)
            .DistinctBy(static item => item.Id)
            .Take(request.Limit)
            .ToArray();
    }

    private async Task<InstanceConfig> LoadConfigAsync(CancellationToken cancellationToken)
    {
        var config = await configProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
        return configSelector(config);
    }

    private async Task<IReadOnlyList<UnifiedTimelineItem>> FetchStatusesAsync(
        Uri baseUri,
        Uri uri,
        TimelineRequest request,
        string paginationKey,
        CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory
            .CreateClient(ServiceCollectionLiveClientNames.LiveFeeds)
            .GetAsync(uri, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var items = document.RootElement
            .EnumerateArray()
            .Select(status => MapStatus(baseUri, status))
            .Where(item => request.Since is null || item.PublishedAt > request.Since)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();

        StoreNextMaxId(paginationKey, request.PageToken, items);
        return items;
    }

    private Uri BuildUri(
        Uri baseUri,
        string path,
        TimelineRequest request,
        string paginationKey)
    {
        var page = ParsePage(request.PageToken);
        if (page == 0)
        {
            ClearPagination(paginationKey);
        }

        var parameters = new List<string>
        {
            $"limit={Math.Clamp(request.Limit, 1, 40)}"
        };

        if (page > 0)
        {
            var previousTokenKey = TokenKey(paginationKey, page);
            if (_maxIdByPage.TryGetValue(previousTokenKey, out var maxId))
            {
                parameters.Add($"max_id={Uri.EscapeDataString(maxId)}");
            }
        }

        return new Uri(baseUri, $"{path}?{string.Join("&", parameters)}");
    }

    private void StoreNextMaxId(
        string paginationKey,
        string? pageToken,
        IReadOnlyList<UnifiedTimelineItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var page = ParsePage(pageToken);
        _maxIdByPage[TokenKey(paginationKey, page + 1)] = items[^1].Id;
    }

    private void ClearPagination(string paginationKey)
    {
        foreach (var key in _maxIdByPage.Keys.Where(key => key.StartsWith(paginationKey + ":", StringComparison.Ordinal)))
        {
            _maxIdByPage.TryRemove(key, out _);
        }
    }

    private static string TokenKey(string paginationKey, int page) => $"{paginationKey}:{page}";

    private static int ParsePage(string? pageToken) =>
        int.TryParse(pageToken, out var page) && page > 0 ? page : 0;

    private UnifiedTimelineItem MapStatus(Uri baseUri, JsonElement status)
    {
        var account = LiveFeedJson.Object(status, "account");
        var accountElement = account ?? default;
        var acct = account is null ? string.Empty : LiveFeedJson.String(accountElement, "acct") ?? string.Empty;
        var handle = string.IsNullOrWhiteSpace(acct)
            ? string.Empty
            : acct.Contains('@', StringComparison.Ordinal) ? $"@{acct}" : $"@{acct}@{baseUri.Host}";

        var tags = LiveFeedJson.Array(status, "tags") is { } tagArray
            ? tagArray.EnumerateArray()
                .Select(tag => LiveFeedJson.String(tag, "name"))
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Select(static name => "#" + name!.TrimStart('#').ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var attachments = LiveFeedJson.Array(status, "media_attachments") is { } mediaArray
            ? mediaArray.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();

        var mediaUrls = attachments
            .Where(static attachment =>
                string.Equals(LiveFeedJson.String(attachment, "type"), "image", StringComparison.OrdinalIgnoreCase))
            .Select(attachment => LiveFeedJson.String(attachment, "url"))
            .Where(static url => !string.IsNullOrWhiteSpace(url))
            .Select(static url => url!)
            .ToArray();

        var thumbnail = attachments
            .Select(attachment => LiveFeedJson.String(attachment, "preview_url"))
            .FirstOrDefault(static url => !string.IsNullOrWhiteSpace(url));
        var videoUrl = attachments
            .Where(static attachment =>
                string.Equals(LiveFeedJson.String(attachment, "type"), "video", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(LiveFeedJson.String(attachment, "type"), "gifv", StringComparison.OrdinalIgnoreCase))
            .Select(attachment => LiveFeedJson.String(attachment, "url"))
            .FirstOrDefault(static url => !string.IsNullOrWhiteSpace(url));

        var content = LiveFeedJson.TextFromHtml(LiveFeedJson.String(status, "content"));
        var id = LiveFeedJson.String(status, "id") ?? Guid.NewGuid().ToString("N");

        return new UnifiedTimelineItem
        {
            Id = id,
            SourceType = SourceType,
            SourceInstance = baseUri.Host,
            AuthorName = account is null
                ? SourceType.ToString()
                : LiveFeedJson.String(accountElement, "display_name")
                    ?? LiveFeedJson.String(accountElement, "username")
                    ?? SourceType.ToString(),
            AuthorHandle = handle,
            AuthorAvatarUrl = account is null ? null : LiveFeedJson.String(accountElement, "avatar"),
            Text = content,
            MediaUrls = mediaUrls,
            ThumbnailUrl = thumbnail,
            VideoUrl = videoUrl,
            ExternalUrl = LiveFeedJson.String(status, "url") ?? LiveFeedJson.String(status, "uri"),
            PublishedAt = LiveFeedJson.Date(LiveFeedJson.String(status, "created_at")),
            Tags = tags,
            Engagement = new EngagementSummary(
                Replies: LiveFeedJson.Int32(status, "replies_count"),
                Likes: LiveFeedJson.Int32(status, "favourites_count"),
                BoostsOrShares: LiveFeedJson.Int32(status, "reblogs_count")),
            CanReply = true,
            CanLike = true,
            CanBoostOrShare = true,
            CanOpenOriginal = true
        };
    }
}
