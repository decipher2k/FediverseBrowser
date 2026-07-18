using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;
using FediverseHub.Infrastructure.Mock;

namespace FediverseHub.Tests;

public sealed class TimelineAggregatorTests
{
    [Fact]
    public async Task Unified_timeline_contains_all_demo_source_types()
    {
        IFediverseSourceClient[] clients =
        [
            new MockMastodonClient(),
            new MockPixelfedClient(),
            new MockPeerTubeClient(),
            new MockLemmyClient(),
            new MockRssSourceClient()
        ];
        var aggregator = new TimelineAggregator(clients);

        var items = await aggregator.GetUnifiedTimelineAsync(
            new TimelineRequest { Limit = 20 },
            CancellationToken.None);

        Assert.Contains(items, item => item.SourceType == FediverseSourceType.Mastodon);
        Assert.Contains(items, item => item.SourceType == FediverseSourceType.Pixelfed);
        Assert.Contains(items, item => item.SourceType == FediverseSourceType.PeerTube);
        Assert.Contains(items, item => item.SourceType == FediverseSourceType.Lemmy);
        Assert.Contains(items, item => item.SourceType == FediverseSourceType.Rss);
    }

    [Fact]
    public async Task Filtered_timeline_is_sorted_by_publish_date_descending()
    {
        var aggregator = new TimelineAggregator(
        [
            new MockMastodonClient(),
            new MockPixelfedClient(),
            new MockPeerTubeClient(),
            new MockLemmyClient(),
            new MockRssSourceClient()
        ]);

        var items = await aggregator.GetUnifiedTimelineAsync(
            new TimelineRequest
            {
                Sources = new HashSet<FediverseSourceType> { FediverseSourceType.Mastodon },
                Limit = 20
            },
            CancellationToken.None);

        Assert.Equal(
            items.OrderByDescending(item => item.PublishedAt).Select(item => item.Id),
            items.Select(item => item.Id));
    }

    [Fact]
    public async Task All_timeline_interleaves_sources_before_applying_limit()
    {
        var aggregator = new TimelineAggregator(
        [
            new StaticSourceClient(FediverseSourceType.Mastodon, DateTimeOffset.Parse("2026-06-17T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
            new StaticSourceClient(FediverseSourceType.Pixelfed, DateTimeOffset.Parse("2026-06-15T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
            new StaticSourceClient(FediverseSourceType.PeerTube, DateTimeOffset.Parse("2026-06-14T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture))
        ]);

        var items = await aggregator.GetUnifiedTimelineAsync(
            new TimelineRequest { Limit = 3 },
            CancellationToken.None);

        Assert.Equal(
            [FediverseSourceType.Mastodon, FediverseSourceType.Pixelfed, FediverseSourceType.PeerTube],
            items.Select(static item => item.SourceType));
    }

    [Fact]
    public async Task Source_filter_returns_only_requested_type()
    {
        var aggregator = new TimelineAggregator(
        [
            new MockMastodonClient(),
            new MockPixelfedClient(),
            new MockPeerTubeClient(),
            new MockLemmyClient(),
            new MockRssSourceClient()
        ]);

        var items = await aggregator.GetUnifiedTimelineAsync(
            new TimelineRequest
            {
                Sources = new HashSet<FediverseSourceType> { FediverseSourceType.Rss },
                Limit = 20
            },
            CancellationToken.None);

        Assert.All(items, item => Assert.Equal(FediverseSourceType.Rss, item.SourceType));
    }

    [Fact]
    public async Task Followed_hashtags_are_loaded_from_hashtag_timelines()
    {
        var client = new RecordingHashtagClient();
        var aggregator = new TimelineAggregator([client]);

        var items = await aggregator.GetUnifiedTimelineAsync(
            new TimelineRequest
            {
                Limit = 5,
                PreferInterestRelevance = true,
                InterestHashtags = ["maker"]
            },
            CancellationToken.None);

        Assert.Contains("#maker", client.RequestedHashtags);
        Assert.Contains(items, item => item.Id == "hashtag-maker");
        Assert.Equal("hashtag-maker", items[0].Id);
    }

    private sealed class RecordingHashtagClient : IFediverseSourceClient
    {
        public List<string> RequestedHashtags { get; } = [];

        public FediverseSourceType SourceType => FediverseSourceType.Mastodon;

        public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(AuthStatus.Demo(SourceType, "@demo@mastodon.social"));

        public Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
            TimelineRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(
            [
                CreateItem(
                    "home-general",
                    DateTimeOffset.Parse("2026-06-15T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                    ["#general"])
            ]);

        public Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
            string hashtag,
            TimelineRequest request,
            CancellationToken cancellationToken)
        {
            RequestedHashtags.Add(hashtag);
            return Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(
                hashtag == "#maker"
                    ?
                    [
                        CreateItem(
                            "hashtag-maker",
                            DateTimeOffset.Parse("2026-06-15T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                            ["#maker"])
                    ]
                    : []);
        }

        public Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new SourceCapabilitySet { SourceType = SourceType });

        private static UnifiedTimelineItem CreateItem(
            string id,
            DateTimeOffset publishedAt,
            IReadOnlyList<string> tags) =>
            new()
            {
                Id = id,
                SourceType = FediverseSourceType.Mastodon,
                SourceInstance = "https://mastodon.social",
                AuthorName = "Demo",
                AuthorHandle = "@demo@mastodon.social",
                Text = id,
                PublishedAt = publishedAt,
                Tags = tags,
                Engagement = new EngagementSummary()
            };
    }

    private sealed class StaticSourceClient(
        FediverseSourceType sourceType,
        DateTimeOffset newestPublishedAt) : IFediverseSourceClient
    {
        public FediverseSourceType SourceType => sourceType;

        public Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(AuthStatus.Demo(SourceType, SourceType.ToString()));

        public Task<IReadOnlyList<UnifiedTimelineItem>> GetTimelineAsync(
            TimelineRequest request,
            CancellationToken cancellationToken)
        {
            var items = Enumerable.Range(0, request.Limit)
                .Select(index => CreateItem(index, newestPublishedAt.AddMinutes(-index)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(items);
        }

        public Task<IReadOnlyList<UnifiedTimelineItem>> GetHashtagTimelineAsync(
            string hashtag,
            TimelineRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnifiedTimelineItem>>(Array.Empty<UnifiedTimelineItem>());

        public Task<SourceCapabilitySet> GetCapabilitiesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new SourceCapabilitySet { SourceType = SourceType });

        private UnifiedTimelineItem CreateItem(int index, DateTimeOffset publishedAt) =>
            new()
            {
                Id = $"{SourceType}-{index}",
                SourceType = SourceType,
                SourceInstance = SourceType.ToString(),
                AuthorName = SourceType.ToString(),
                AuthorHandle = $"@{SourceType}",
                Text = $"{SourceType} {index}",
                PublishedAt = publishedAt,
                Engagement = new EngagementSummary()
            };
    }
}
