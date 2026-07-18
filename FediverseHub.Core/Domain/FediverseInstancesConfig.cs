namespace FediverseHub.Core.Domain;

public sealed record FediverseInstancesConfig
{
    public InstanceConfig Mastodon { get; init; } = InstanceConfig.Empty(FediverseSourceType.Mastodon);
    public InstanceConfig Pixelfed { get; init; } = InstanceConfig.Empty(FediverseSourceType.Pixelfed);
    public InstanceConfig PeerTube { get; init; } = InstanceConfig.Empty(FediverseSourceType.PeerTube);
    public InstanceConfig Lemmy { get; init; } = InstanceConfig.Empty(FediverseSourceType.Lemmy);
    public IReadOnlyList<RssFeedDefinition> RssFeeds { get; init; } = Array.Empty<RssFeedDefinition>();
}

public sealed record InstanceConfig
{
    public required FediverseSourceType SourceType { get; init; }
    public required string BaseUrl { get; init; }
    public string? ClientId { get; init; }
    public string? RedirectUri { get; init; }
    public bool PreferExternalRegistration { get; init; }

    public static InstanceConfig Empty(FediverseSourceType sourceType) =>
        new() { SourceType = sourceType, BaseUrl = string.Empty };
}

public sealed record RssFeedDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public bool IsEnabled { get; init; } = true;
}
