namespace FediverseHub.Core.Domain;

public sealed record AppSettings
{
    public string Theme { get; init; } = "system";
    public string MediaAutoplay { get; init; } = "wifi";
    public string? LanguageCode { get; init; }
    public bool UseSystemLanguage { get; init; } = true;
    public IReadOnlyList<string> FollowedHashtags { get; init; } = Array.Empty<string>();
}
