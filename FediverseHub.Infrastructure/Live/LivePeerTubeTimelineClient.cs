using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Infrastructure.Live;

public sealed class LivePeerTubeTimelineClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider) : IFediverseSourceClient
{
    public FediverseSourceType SourceType => FediverseSourceType.PeerTube;

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AuthStatus.Demo(SourceType, SourceType.ToString()));

    public Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new SourceCapabilitySet
        {
            SourceType = SourceType,
            SupportsPosting = false,
            SupportsMediaUpload = false,
            SupportsHashtagFollowing = false
        });

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
        TimelineRequest request,
        CancellationToken cancellationToken) =>
        await FetchAsync(request, null, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        return normalized.Length == 0
            ? Array.Empty<UnifiedTimelineItem>()
            : await FetchAsync(request, normalized, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<UnifiedTimelineItem>> FetchAsync(
        TimelineRequest request,
        string? hashtag,
        CancellationToken cancellationToken)
    {
        var instances = await configProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
        var config = instances.PeerTube;
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var baseUri = LiveFeedJson.BaseUri(config.BaseUrl);
        var page = ParsePage(request.PageToken);
        var query = new List<string>
        {
            "sort=-publishedAt",
            $"count={Math.Clamp(request.Limit, 1, 100)}",
            $"start={Math.Max(0, page * request.Limit)}"
        };

        if (hashtag is not null)
        {
            query.Add($"tagsOneOf={Uri.EscapeDataString(hashtag.TrimStart('#'))}");
        }

        var uri = new Uri(baseUri, $"api/v1/videos?{string.Join("&", query)}");
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

        if (LiveFeedJson.Array(document.RootElement, "data") is not { } data)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        return data.EnumerateArray()
            .Select(video => MapVideo(baseUri, video))
            .Where(item => request.Since is null || item.PublishedAt > request.Since)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();
    }

    private static UnifiedTimelineItem MapVideo(Uri baseUri, JsonElement video)
    {
        var account = LiveFeedJson.Object(video, "account");
        var channel = LiveFeedJson.Object(video, "channel");
        var authorObject = account ?? channel;
        var authorElement = authorObject ?? default;
        var id = LiveFeedJson.String(video, "uuid")
            ?? LiveFeedJson.Int32(video, "id").ToString(System.Globalization.CultureInfo.InvariantCulture);
        var tags = LiveFeedJson.Array(video, "tags") is { } tagArray
            ? tagArray.EnumerateArray()
                .Select(TagValue)
                .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                .Select(static tag => "#" + tag!.TrimStart('#').ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var thumbnail = LiveFeedJson.String(video, "thumbnailPath")
            ?? LiveFeedJson.String(video, "previewPath");
        var externalUrl = LiveFeedJson.String(video, "url") ?? new Uri(baseUri, $"w/{id}").ToString();
        var embedUrl = BuildEmbedUrl(baseUri, externalUrl, LiveFeedJson.String(video, "embedPath"), id);

        return new UnifiedTimelineItem
        {
            Id = id,
            SourceType = FediverseSourceType.PeerTube,
            SourceInstance = baseUri.Host,
            AuthorName = authorObject is null
                ? "PeerTube"
                : LiveFeedJson.String(authorElement, "displayName")
                    ?? LiveFeedJson.String(authorElement, "name")
                    ?? "PeerTube",
            AuthorHandle = authorObject is null
                ? string.Empty
                : BuildHandle(baseUri, authorElement),
            AuthorAvatarUrl = LiveFeedJson.String(authorElement, "avatarUrl"),
            Title = LiveFeedJson.String(video, "name"),
            Text = LiveFeedJson.TextFromHtml(LiveFeedJson.String(video, "description")),
            ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnail)
                ? null
                : LiveFeedJson.AbsoluteUrl(baseUri, thumbnail),
            VideoUrl = embedUrl,
            ExternalUrl = externalUrl,
            PublishedAt = LiveFeedJson.Date(
                LiveFeedJson.String(video, "publishedAt") ?? LiveFeedJson.String(video, "createdAt")),
            Tags = tags,
            Engagement = new EngagementSummary(
                Likes: LiveFeedJson.Int32(video, "likes"),
                Views: LiveFeedJson.Int32(video, "views")),
            CanReply = true,
            CanLike = true,
            CanBoostOrShare = true,
            CanOpenOriginal = true
        };
    }

    private static string BuildEmbedUrl(Uri baseUri, string externalUrl, string? embedPath, string videoId)
    {
        var embedBaseUri = Uri.TryCreate(externalUrl, UriKind.Absolute, out var externalUri)
            ? new Uri(externalUri.GetLeftPart(UriPartial.Authority) + "/")
            : baseUri;

        return string.IsNullOrWhiteSpace(embedPath)
            ? new Uri(embedBaseUri, $"videos/embed/{videoId}").ToString()
            : LiveFeedJson.AbsoluteUrl(embedBaseUri, embedPath);
    }

    private static string BuildHandle(Uri baseUri, JsonElement author)
    {
        var name = LiveFeedJson.String(author, "name");
        var host = LiveFeedJson.String(author, "host") ?? baseUri.Host;
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"@{name}@{host}";
    }

    private static string? TagValue(JsonElement tag) =>
        tag.ValueKind switch
        {
            JsonValueKind.String => tag.GetString(),
            JsonValueKind.Object => LiveFeedJson.String(tag, "name"),
            _ => null
        };

    private static int ParsePage(string? pageToken) =>
        int.TryParse(pageToken, out var page) && page > 0 ? page : 0;
}
