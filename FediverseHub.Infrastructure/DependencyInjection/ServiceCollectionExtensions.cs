using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;
using FediverseHub.Core.ViewModels;
using FediverseHub.Infrastructure.Configuration;
using FediverseHub.Infrastructure.Live;
using FediverseHub.Infrastructure.Localization;
using FediverseHub.Infrastructure.Mock;
using FediverseHub.Infrastructure.Persistence;
using FediverseHub.Infrastructure.Rss;
using FediverseHub.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace FediverseHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFediverseHubInfrastructure(
        this IServiceCollection services,
        string? dataDirectory = null,
        string? configPath = null)
    {
        services.AddHttpClient(ServiceCollectionLiveClientNames.LiveFeeds, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FediverseHub/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/activity+json");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
        });
        services.AddSingleton(new SqliteConnectionFactory(dataDirectory));
        services.AddSingleton<SqliteSchemaInitializer>();
        services.AddSingleton<ISettingsStore, SqliteSettingsStore>();
        services.AddSingleton<ITimelineRepository, SqliteTimelineRepository>();
        services.AddSingleton<IRssFeedRepository, SqliteRssFeedRepository>();
        services.AddSingleton<ISecureTokenStore>(_ => new EncryptedFileTokenStore(dataDirectory));
        services.AddSingleton<IInstanceConfigProvider>(_ => new JsonInstanceConfigProvider(configPath));
        services.AddSingleton<IRssFeedParser, SyndicationRssFeedParser>();
        services.AddSingleton<ISystemCultureProvider, SystemCultureProvider>();
        services.AddSingleton<ILocalizationService, JsonLocalizationService>();

        services.AddSingleton<InterestCatalog>();
        services.AddSingleton<ComposePostValidator>();
        services.AddSingleton<IAppSession, AppSession>();
        services.AddSingleton<ITimelineAggregator, TimelineAggregator>();
        services.AddSingleton<IHashtagFollowService, HashtagFollowService>();

        services.AddSingleton<LiveMastodonTimelineClient>();
        services.AddSingleton<LivePixelfedTimelineClient>();
        services.AddSingleton<LivePeerTubeTimelineClient>();
        services.AddSingleton<LiveLemmyTimelineClient>();
        services.AddSingleton<LiveRssSourceClient>();

        services.AddSingleton<MockMastodonClient>();
        services.AddSingleton<IMastodonClient>(sp => sp.GetRequiredService<MockMastodonClient>());
        services.AddSingleton<IPostPublisher>(sp => sp.GetRequiredService<MockMastodonClient>());
        services.AddSingleton<IHashtagRemoteFollowClient>(sp => sp.GetRequiredService<MockMastodonClient>());
        services.AddSingleton<IRegistrationClient>(sp => sp.GetRequiredService<MockMastodonClient>());
        services.AddSingleton<IFediverseSourceClient>(sp => sp.GetRequiredService<MockMastodonClient>());

        services.AddSingleton<MockPixelfedClient>();
        services.AddSingleton<IPixelfedClient>(sp => sp.GetRequiredService<MockPixelfedClient>());
        services.AddSingleton<IPostPublisher>(sp => sp.GetRequiredService<MockPixelfedClient>());
        services.AddSingleton<IHashtagRemoteFollowClient>(sp => sp.GetRequiredService<MockPixelfedClient>());
        services.AddSingleton<IRegistrationClient>(sp => sp.GetRequiredService<MockPixelfedClient>());
        services.AddSingleton<IFediverseSourceClient>(sp => sp.GetRequiredService<MockPixelfedClient>());

        services.AddSingleton<MockPeerTubeClient>();
        services.AddSingleton<IPeerTubeClient>(sp => sp.GetRequiredService<MockPeerTubeClient>());
        services.AddSingleton<IPostPublisher>(sp => sp.GetRequiredService<MockPeerTubeClient>());
        services.AddSingleton<IRegistrationClient>(sp => sp.GetRequiredService<MockPeerTubeClient>());
        services.AddSingleton<IFediverseSourceClient>(sp => sp.GetRequiredService<MockPeerTubeClient>());

        services.AddSingleton<MockLemmyClient>();
        services.AddSingleton<ILemmyClient>(sp => sp.GetRequiredService<MockLemmyClient>());
        services.AddSingleton<IPostPublisher>(sp => sp.GetRequiredService<MockLemmyClient>());
        services.AddSingleton<IRegistrationClient>(sp => sp.GetRequiredService<MockLemmyClient>());
        services.AddSingleton<IFediverseSourceClient>(sp => sp.GetRequiredService<MockLemmyClient>());

        services.AddSingleton<MockRssSourceClient>();
        services.AddSingleton<IRssSourceClient>(sp => sp.GetRequiredService<MockRssSourceClient>());
        services.AddSingleton<IFediverseSourceClient>(sp => sp.GetRequiredService<MockRssSourceClient>());

        services.AddTransient<TimelineViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<ComposeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LoginRegistrationViewModel>();

        return services;
    }
}
