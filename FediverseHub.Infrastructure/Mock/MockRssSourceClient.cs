using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Mock;

public sealed class MockRssSourceClient : MockSourceClientBase, IRssSourceClient
{
    private const string InstanceUrl = "https://fediversehub.local/rss";
    private static readonly DateTimeOffset BaseTime = new(2026, 06, 15, 10, 00, 00, TimeSpan.Zero);

    public MockRssSourceClient()
        : base(
            FediverseSourceType.Rss,
            InstanceUrl,
            new SourceCapabilitySet
            {
                SourceType = FediverseSourceType.Rss,
                SupportsPosting = false,
                SupportsHashtagFollowing = false,
                SupportsMediaUpload = false,
                SupportsReporting = false,
                SupportsBlocking = false
            },
            CreateItems())
    {
    }

    private static IReadOnlyList<UnifiedTimelineItem> CreateItems() =>
    [
        Item(
            "rss-1",
            FediverseSourceType.Rss,
            "Fediverse Report",
            "Fediverse Report",
            "https://fediversereport.com/rss",
            "Weekly fediverse moderation and growth notes",
            "A concise roundup of protocol work, instance policies and migration stories.",
            BaseTime.AddMinutes(15),
            ["#fediverse", "#news", "#journalism"],
            new EngagementSummary(),
            thumbnail: "https://images.unsplash.com/photo-1504711434969-e33886168f5c",
            external: "https://fediversereport.com/",
            interactive: false),
        Item(
            "rss-2",
            FediverseSourceType.Rss,
            "Open Source News",
            "Open Source News",
            "https://opensource.com/feed",
            "Maintaining tiny tools over the long term",
            "Practical advice for funding, documentation and issue triage.",
            BaseTime.AddMinutes(-6),
            ["#opensource", "#programming", "#community"],
            new EngagementSummary(),
            thumbnail: "https://images.unsplash.com/photo-1515879218367-8466d910aaa4",
            external: "https://opensource.com/",
            interactive: false)
    ];
}
