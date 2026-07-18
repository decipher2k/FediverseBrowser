using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Infrastructure.Live;

public sealed class LiveLemmyTimelineClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider) : IFediverseSourceClient
{
    public FediverseSourceType SourceType => FediverseSourceType.Lemmy;

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
        var config = instances.Lemmy;
        if (string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var baseUri = LiveFeedJson.BaseUri(config.BaseUrl);
        var page = ParsePage(request.PageToken) + 1;
        var limit = Math.Clamp(request.Limit, 1, 50);
        var path = hashtag is null
            ? $"api/v3/post/list?sort=New&type_=All&limit={limit}&page={page}"
            : $"api/v3/search?sort=New&type_=Posts&listing_type=All&q={Uri.EscapeDataString(hashtag.TrimStart('#'))}&limit={limit}&page={page}";
        var uri = new Uri(baseUri, path);

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

        if (LiveFeedJson.Array(document.RootElement, "posts") is not { } posts)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        return posts.EnumerateArray()
            .Select(postView => MapPost(baseUri, postView))
            .Where(item => request.Since is null || item.PublishedAt > request.Since)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();
    }

    private static UnifiedTimelineItem MapPost(Uri baseUri, JsonElement postView)
    {
        var post = LiveFeedJson.Object(postView, "post") ?? postView;
        var creator = LiveFeedJson.Object(postView, "creator");
        var community = LiveFeedJson.Object(postView, "community");
        var counts = LiveFeedJson.Object(postView, "counts");
        var creatorElement = creator ?? default;
        var countsElement = counts ?? default;
        var title = LiveFeedJson.String(post, "name") ?? "Lemmy post";
        var body = LiveFeedJson.String(post, "body") ?? string.Empty;
        var id = LiveFeedJson.Int32(post, "id").ToString(System.Globalization.CultureInfo.InvariantCulture);
        var external = LiveFeedJson.String(post, "ap_id")
            ?? LiveFeedJson.String(post, "url")
            ?? new Uri(baseUri, $"post/{id}").ToString();
        var tagSource = string.Join(' ', title, body, community is null ? string.Empty : LiveFeedJson.String(community.Value, "name"));

        return new UnifiedTimelineItem
        {
            Id = id,
            SourceType = FediverseSourceType.Lemmy,
            SourceInstance = baseUri.Host,
            AuthorName = creator is null
                ? "Lemmy"
                : LiveFeedJson.String(creatorElement, "display_name")
                    ?? LiveFeedJson.String(creatorElement, "name")
                    ?? "Lemmy",
            AuthorHandle = creator is null ? string.Empty : BuildHandle(baseUri, creatorElement),
            AuthorAvatarUrl = creator is null ? null : LiveFeedJson.String(creatorElement, "avatar"),
            Title = title,
            Text = body,
            ThumbnailUrl = LiveFeedJson.String(post, "thumbnail_url"),
            ExternalUrl = external,
            PublishedAt = LiveFeedJson.Date(LiveFeedJson.String(post, "published")),
            Tags = LiveFeedJson.ExtractHashtags(tagSource),
            Engagement = new EngagementSummary(
                Replies: LiveFeedJson.Int32(countsElement, "comments"),
                Upvotes: LiveFeedJson.Int32(countsElement, "upvotes"),
                Downvotes: LiveFeedJson.Int32(countsElement, "downvotes")),
            CanReply = true,
            CanLike = true,
            CanBoostOrShare = true,
            CanOpenOriginal = true
        };
    }

    private static string BuildHandle(Uri baseUri, JsonElement creator)
    {
        var actorId = LiveFeedJson.String(creator, "actor_id");
        if (Uri.TryCreate(actorId, UriKind.Absolute, out var actorUri))
        {
            var nameFromPath = actorUri.AbsolutePath.Trim('/').Split('/').LastOrDefault();
            return string.IsNullOrWhiteSpace(nameFromPath) ? string.Empty : $"@{nameFromPath}@{actorUri.Host}";
        }

        var name = LiveFeedJson.String(creator, "name");
        return string.IsNullOrWhiteSpace(name) ? string.Empty : $"@{name}@{baseUri.Host}";
    }

    private static int ParsePage(string? pageToken) =>
        int.TryParse(pageToken, out var page) && page > 0 ? page : 0;
}
