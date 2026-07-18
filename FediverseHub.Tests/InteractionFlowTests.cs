using System.Globalization;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;
using FediverseHub.Core.ViewModels;
using FediverseHub.Infrastructure.Mock;

namespace FediverseHub.Tests;

public sealed class InteractionFlowTests
{
    [Fact]
    public async Task Timeline_load_more_appends_new_items_without_duplicates()
    {
        IFediverseSourceClient[] clients =
        [
            new MockMastodonClient(),
            new MockPixelfedClient(),
            new MockPeerTubeClient(),
            new MockLemmyClient(),
            new MockRssSourceClient()
        ];
        var viewModel = new TimelineViewModel(
            new TimelineAggregator(clients),
            new EmptyHashtagFollowService(),
            new EchoLocalizationService());

        await viewModel.RefreshAsync(CancellationToken.None);
        var initialCount = viewModel.Items.Count;

        await viewModel.LoadMoreAsync(CancellationToken.None);

        Assert.True(initialCount >= 20);
        Assert.True(viewModel.Items.Count > initialCount);
        Assert.Equal(
            viewModel.Items.Count,
            viewModel.Items.Select(item => $"{item.Item.SourceType}:{item.Item.Id}").Distinct().Count());
    }

    [Fact]
    public async Task Read_only_session_blocks_publishing()
    {
        var session = new AppSession();
        session.StartReadOnlySession();
        var mastodon = new MockMastodonClient();
        var viewModel = new ComposeViewModel(
            [mastodon],
            [mastodon],
            new ComposePostValidator(),
            new EchoLocalizationService(),
            session)
        {
            Text = "Hello #fediverse"
        };

        await viewModel.PublishAsync(CancellationToken.None);

        Assert.Equal("error.readOnlyMode", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Authenticated_session_allows_mock_publish()
    {
        var session = new AppSession();
        session.StartAuthenticatedSession();
        var mastodon = new MockMastodonClient();
        var viewModel = new ComposeViewModel(
            [mastodon],
            [mastodon],
            new ComposePostValidator(),
            new EchoLocalizationService(),
            session)
        {
            Text = "Hello #fediverse"
        };

        await viewModel.PublishAsync(CancellationToken.None);

        Assert.Equal("compose.published", viewModel.StatusMessage);
    }

    [Fact]
    public void Sign_out_returns_session_to_read_only_mode()
    {
        var session = new AppSession();
        session.StartAuthenticatedSession();

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.True(session.IsReadOnly);
    }

    private sealed class EmptyHashtagFollowService : IHashtagFollowService
    {
        public Task FollowHashtagsAsync(IEnumerable<string> hashtags, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UnfollowHashtagAsync(string hashtag, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetFollowedHashtagsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class EchoLocalizationService : ILocalizationService
    {
        public CultureInfo CurrentCulture => CultureInfo.InvariantCulture;

        public IReadOnlyList<SupportedLanguage> SupportedLanguages => Array.Empty<SupportedLanguage>();

        public string GetString(string key) => key;

        public Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UseSystemLanguageAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
