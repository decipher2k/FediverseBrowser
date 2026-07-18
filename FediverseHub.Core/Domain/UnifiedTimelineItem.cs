namespace FediverseHub.Core.Domain;

public sealed record UnifiedTimelineItem
{
    public required string Id { get; init; }
    public required FediverseSourceType SourceType { get; init; }
    public required string SourceInstance { get; init; }
    public required string AuthorName { get; init; }
    public required string AuthorHandle { get; init; }
    public string? AuthorAvatarUrl { get; init; }
    public string? Title { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<string> MediaUrls { get; init; } = Array.Empty<string>();
    public string? ThumbnailUrl { get; init; }
    public string? VideoUrl { get; init; }
    public string? ExternalUrl { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public EngagementSummary Engagement { get; init; } = new();
    public bool CanReply { get; init; }
    public bool CanLike { get; init; }
    public bool CanBoostOrShare { get; init; }
    public bool CanOpenOriginal { get; init; } = true;
    public string? LocalState { get; init; }
}
