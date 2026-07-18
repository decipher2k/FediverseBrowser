using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Mock;

public sealed class MockPixelfedClient : MockSourceClientBase, IPixelfedClient
{
    private const string InstanceUrl = "https://pixelfed.social";
    private static readonly DateTimeOffset BaseTime = new(2026, 06, 15, 10, 00, 00, TimeSpan.Zero);

    public MockPixelfedClient()
        : base(
            FediverseSourceType.Pixelfed,
            InstanceUrl,
            new SourceCapabilitySet
            {
                SourceType = FediverseSourceType.Pixelfed,
                SupportsPosting = true,
                SupportsHashtagFollowing = true,
                SupportsMediaUpload = true,
                SupportsReporting = true,
                SupportsBlocking = true,
                MaxMediaBytes = 24 * 1024 * 1024,
                AllowedMediaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/png",
                    "image/webp"
                }
            },
            CreateItems())
    {
    }

    private static IReadOnlyList<UnifiedTimelineItem> CreateItems() =>
    [
        Item(
            "pixelfed-1",
            FediverseSourceType.Pixelfed,
            InstanceUrl,
            "Mina Streets",
            "@mina@pixelfed.social",
            "Rain on neon glass",
            "A night walk through the station district.",
            BaseTime.AddMinutes(30),
            ["#photography", "#streetphotography", "#art"],
            new EngagementSummary(Replies: 7, Likes: 311, BoostsOrShares: 12),
            thumbnail: "https://images.unsplash.com/photo-1493246507139-91e8fad9978e",
            media: ["https://images.unsplash.com/photo-1493246507139-91e8fad9978e"]),
        Item(
            "pixelfed-2",
            FediverseSourceType.Pixelfed,
            InstanceUrl,
            "Kitchen Notes",
            "@kitchen@pixelfed.social",
            "Summer bowl",
            "Tomatoes, herbs, grilled bread and a very patient olive oil.",
            BaseTime.AddMinutes(4),
            ["#food", "#cooking", "#recipes"],
            new EngagementSummary(Replies: 12, Likes: 204, BoostsOrShares: 6),
            thumbnail: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c",
            media: ["https://images.unsplash.com/photo-1546069901-ba9599a7e63c"])
    ];
}
