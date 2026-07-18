namespace FediverseHub.Core.Domain;

public sealed record ComposePostDraft
{
    public required FediverseSourceType TargetSource { get; init; }
    public string? Title { get; init; }
    public string? Text { get; init; }
    public IReadOnlyList<MediaAttachmentDraft> Media { get; init; } = Array.Empty<MediaAttachmentDraft>();
    public string? AltText { get; init; }
    public string? VideoDescription { get; init; }
    public string? CommunityName { get; init; }
    public string? Visibility { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record MediaAttachmentDraft
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string? LocalPath { get; init; }
}
