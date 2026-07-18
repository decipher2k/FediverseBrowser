using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;

namespace FediverseHub.Infrastructure.Live;

public sealed class LiveRssSourceClient(
    IHttpClientFactory httpClientFactory,
    IInstanceConfigProvider configProvider,
    IRssFeedParser feedParser) : IRssSourceClient
{
    public FediverseSourceType SourceType => FediverseSourceType.Rss;

    public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AuthStatus.Demo(SourceType, "rss"));

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
        if (ParsePage(request.PageToken) > 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var items = await FetchConfiguredFeedsAsync(cancellationToken).ConfigureAwait(false);
        return items
            .Where(item => request.Since is null || item.PublishedAt > request.Since)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();
    }

    public async Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
        string hashtag,
        TimelineRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        if (normalized.Length == 0 || ParsePage(request.PageToken) > 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var items = await FetchConfiguredFeedsAsync(cancellationToken).ConfigureAwait(false);
        return items
            .Where(item => item.Tags.Any(tag =>
                string.Equals(HashtagNormalizer.Normalize(tag), normalized, StringComparison.OrdinalIgnoreCase)) ||
                ContainsHashtag(item.Title, normalized) ||
                ContainsHashtag(item.Text, normalized))
            .Where(item => request.Since is null || item.PublishedAt > request.Since)
            .OrderByDescending(static item => item.PublishedAt)
            .Take(request.Limit)
            .ToArray();
    }

    private async Task<IReadOnlyList<UnifiedTimelineItem>> FetchConfiguredFeedsAsync(CancellationToken cancellationToken)
    {
        var config = await configProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
        var feeds = config.RssFeeds.Where(static feed => feed.IsEnabled).ToArray();
        if (feeds.Length == 0)
        {
            return Array.Empty<UnifiedTimelineItem>();
        }

        var tasks = feeds.Select(feed => FetchFeedSafelyAsync(feed, cancellationToken));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.SelectMany(static result => result).ToArray();
    }

    private async Task<IReadOnlyList<UnifiedTimelineItem>> FetchFeedSafelyAsync(
        RssFeedDefinition feed,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClientFactory
                .CreateClient(ServiceCollectionLiveClientNames.LiveFeeds)
                .GetAsync(feed.Url, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<UnifiedTimelineItem>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await feedParser.ParseAsync(feed, stream, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Array.Empty<UnifiedTimelineItem>();
        }
    }

    private static bool ContainsHashtag(string? value, string normalizedHashtag) =>
        LiveFeedJson.ExtractHashtags(value).Contains(normalizedHashtag, StringComparer.OrdinalIgnoreCase);

    private static int ParsePage(string? pageToken) =>
        int.TryParse(pageToken, out var page) && page > 0 ? page : 0;
}
