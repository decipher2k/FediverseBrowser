namespace FediverseHub.Core.Domain;

public sealed record TimelineRequest
{
    public IReadOnlySet<FediverseSourceType>? Sources { get; init; }
    public int Limit { get; init; } = 40;
    public string? PageToken { get; init; }
    public DateTimeOffset? Since { get; init; }
    public bool PreferInterestRelevance { get; init; }
    public IReadOnlyList<string> InterestHashtags { get; init; } = Array.Empty<string>();
}
