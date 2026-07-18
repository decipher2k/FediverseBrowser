using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Mock;

public sealed class MockLemmyClient : MockSourceClientBase, ILemmyClient
{
    private const string InstanceUrl = "https://lemmy.world";
    private static readonly DateTimeOffset BaseTime = new(2026, 06, 15, 10, 00, 00, TimeSpan.Zero);

    public MockLemmyClient()
        : base(
            FediverseSourceType.Lemmy,
            InstanceUrl,
            new SourceCapabilitySet
            {
                SourceType = FediverseSourceType.Lemmy,
                SupportsPosting = true,
                SupportsHashtagFollowing = false,
                SupportsMediaUpload = false,
                SupportsReporting = true,
                SupportsBlocking = true,
                MaxMediaBytes = 0
            },
            CreateItems())
    {
    }

    private static IReadOnlyList<UnifiedTimelineItem> CreateItems() =>
    [
        Item(
            "lemmy-1",
            FediverseSourceType.Lemmy,
            InstanceUrl,
            "rhea",
            "@rhea@lemmy.world",
            "What are your favorite small .NET libraries?",
            "Collecting focused libraries that do one thing well for desktop apps.",
            BaseTime.AddMinutes(8),
            ["#dotnet", "#programming", "#opensource"],
            new EngagementSummary(Replies: 36, Upvotes: 214, Downvotes: 4),
            external: $"{InstanceUrl}/post/1001"),
        Item(
            "lemmy-2",
            FediverseSourceType.Lemmy,
            InstanceUrl,
            "moderator",
            "@mod@lemmy.world",
            "Community thread: sustainable hosting",
            "Share your notes on small-instance moderation, backups and energy-aware hosting.",
            BaseTime.AddMinutes(-22),
            ["#fediverse", "#sustainability", "#community"],
            new EngagementSummary(Replies: 21, Upvotes: 96, Downvotes: 2),
            external: $"{InstanceUrl}/post/1002")
    ];
}
