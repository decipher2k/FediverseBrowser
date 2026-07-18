using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Mock;

public sealed class MockPeerTubeClient : MockSourceClientBase, IPeerTubeClient
{
    private const string InstanceUrl = "https://video.blender.org";
    private static readonly DateTimeOffset BaseTime = new(2026, 06, 15, 10, 00, 00, TimeSpan.Zero);

    public MockPeerTubeClient()
        : base(
            FediverseSourceType.PeerTube,
            InstanceUrl,
            new SourceCapabilitySet
            {
                SourceType = FediverseSourceType.PeerTube,
                SupportsPosting = true,
                SupportsHashtagFollowing = false,
                SupportsMediaUpload = true,
                SupportsReporting = true,
                SupportsBlocking = false,
                MaxMediaBytes = 512 * 1024 * 1024,
                AllowedMediaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "video/mp4",
                    "video/webm",
                    "video/quicktime"
                }
            },
            CreateItems())
    {
    }

    private static IReadOnlyList<UnifiedTimelineItem> CreateItems() =>
    [
        Item(
            "peertube-1",
            FediverseSourceType.PeerTube,
            InstanceUrl,
            "Blender Studio",
            "@studio@video.blender.org",
            "Open movie pipeline walkthrough",
            "A practical walkthrough of asset publishing, review and rendering in an open production workflow.",
            BaseTime.AddMinutes(18),
            ["#opensource", "#video", "#design"],
            new EngagementSummary(Views: 604),
            thumbnail: "https://images.unsplash.com/photo-1492691527719-9d1e07e534b4",
            video: $"{InstanceUrl}/w/mock-open-movie",
            external: $"{InstanceUrl}/w/mock-open-movie"),
        Item(
            "peertube-2",
            FediverseSourceType.PeerTube,
            InstanceUrl,
            "Science Streams",
            "@science@video.blender.org",
            "The quiet math behind orbital rendezvous",
            "A visual explainer for matching orbits without wasting fuel.",
            BaseTime.AddMinutes(-11),
            ["#science", "#space", "#education"],
            new EngagementSummary(Views: 1280),
            thumbnail: "https://images.unsplash.com/photo-1446776811953-b23d57bd21aa",
            video: $"{InstanceUrl}/w/mock-orbits",
            external: $"{InstanceUrl}/w/mock-orbits")
    ];
}
