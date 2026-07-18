namespace FediverseHub.Core.Domain;

public sealed record InterestCategory
{
    public required string Id { get; init; }
    public required string LocalizationKey { get; init; }
    public required IReadOnlyList<string> Hashtags { get; init; }
}
