namespace FediverseHub.Core.Domain;

public sealed record SourceCapabilitySet
{
    public required FediverseSourceType SourceType { get; init; }
    public bool SupportsPosting { get; init; }
    public bool SupportsHashtagFollowing { get; init; }
    public bool SupportsMediaUpload { get; init; }
    public bool SupportsReporting { get; init; }
    public bool SupportsBlocking { get; init; }
    public long MaxMediaBytes { get; init; } = 20 * 1024 * 1024;
    public IReadOnlySet<string> AllowedMediaTypes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
