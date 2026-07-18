using System.Text.Json;
using System.Text.Json.Serialization;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Configuration;

public sealed class JsonInstanceConfigProvider(string? configPath = null) : IInstanceConfigProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static JsonInstanceConfigProvider()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly string _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, "fediverse.instances.json");

    public async Task<FediverseInstancesConfig> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_configPath))
        {
            return CreateDemoConfig();
        }

        await using var stream = File.OpenRead(_configPath);
        var config = await JsonSerializer.DeserializeAsync<FediverseInstancesConfig>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        return config ?? CreateDemoConfig();
    }

    public static FediverseInstancesConfig CreateDemoConfig() => new()
    {
        Mastodon = new InstanceConfig
        {
            SourceType = FediverseSourceType.Mastodon,
            BaseUrl = "https://mastodon.social",
            RedirectUri = "fediversehub://oauth/mastodon"
        },
        Pixelfed = new InstanceConfig
        {
            SourceType = FediverseSourceType.Pixelfed,
            BaseUrl = "https://pixelfed.social",
            RedirectUri = "fediversehub://oauth/pixelfed"
        },
        PeerTube = new InstanceConfig
        {
            SourceType = FediverseSourceType.PeerTube,
            BaseUrl = "https://video.blender.org",
            PreferExternalRegistration = true
        },
        Lemmy = new InstanceConfig
        {
            SourceType = FediverseSourceType.Lemmy,
            BaseUrl = "https://lemmy.world",
            PreferExternalRegistration = true
        },
        RssFeeds =
        [
            new RssFeedDefinition
            {
                Id = "fediverse-report",
                Title = "Fediverse Report",
                Url = "https://fediversereport.com/rss"
            },
            new RssFeedDefinition
            {
                Id = "open-source-news",
                Title = "Open Source News",
                Url = "https://opensource.com/feed"
            }
        ]
    };
}
