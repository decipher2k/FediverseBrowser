using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Mock;

public sealed class MockMastodonClient : MockSourceClientBase, IMastodonClient
{
    private const string InstanceUrl = "https://mastodon.social";
    private static readonly DateTimeOffset BaseTime = new(2026, 06, 15, 10, 00, 00, TimeSpan.Zero);

    public MockMastodonClient()
        : base(
            FediverseSourceType.Mastodon,
            InstanceUrl,
            new SourceCapabilitySet
            {
                SourceType = FediverseSourceType.Mastodon,
                SupportsPosting = true,
                SupportsHashtagFollowing = true,
                SupportsMediaUpload = true,
                SupportsReporting = true,
                SupportsBlocking = true,
                MaxMediaBytes = 16 * 1024 * 1024,
                AllowedMediaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/png",
                    "image/webp",
                    "image/gif"
                }
            },
            CreateItems())
    {
    }

    private static IReadOnlyList<UnifiedTimelineItem> CreateItems() =>
    [
        Item(
            "mastodon-1",
            FediverseSourceType.Mastodon,
            InstanceUrl,
            "Ada Lovelace",
            "@ada@mastodon.social",
            null,
            "A small thread about federated timelines, humane defaults, and why local cache matters.",
            BaseTime.AddMinutes(22),
            ["#fediverse", "#opensource", "#dotnet"],
            new EngagementSummary(Replies: 18, Likes: 142, BoostsOrShares: 41)),
        Item(
            "mastodon-2",
            FediverseSourceType.Mastodon,
            InstanceUrl,
            "Open Web Lab",
            "@openweb@mastodon.social",
            null,
            "ActivityPub interoperability testing notes are out. Mastodon, Pixelfed and Lemmy all expose different sharp edges.",
            BaseTime.AddMinutes(12),
            ["#activitypub", "#openweb", "#testing"],
            new EngagementSummary(Replies: 9, Likes: 88, BoostsOrShares: 27))
    ];
}
