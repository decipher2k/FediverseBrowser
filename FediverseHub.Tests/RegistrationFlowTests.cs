using System.Globalization;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;
using FediverseHub.Core.ViewModels;
using FediverseHub.Infrastructure.Configuration;
using FediverseHub.Infrastructure.Mock;

namespace FediverseHub.Tests;

public sealed class RegistrationFlowTests
{
    [Theory]
    [InlineData("username", "username.peer")]
    [InlineData("@User_Name", "user_name.peer")]
    [InlineData("username.peer", "username.peer")]
    [InlineData("bad name!", "")]
    public void Builds_peer_account_name(string input, string expected)
    {
        Assert.Equal(expected, RegistrationViewModel.BuildPeerAccountName(input));
    }

    [Fact]
    public async Task Registers_peer_account_on_all_configured_providers()
    {
        var viewModel = new RegistrationViewModel(
            [
                new MockMastodonClient(),
                new MockPixelfedClient(),
                new MockPeerTubeClient(),
                new MockLemmyClient()
            ],
            new DemoConfigProvider(),
            new EchoLocalizationService(),
            new AppSession())
        {
            AccountName = "username",
            Email = "user@example.com",
            Password = "correct-horse-42",
            ConfirmPassword = "correct-horse-42"
        };

        await viewModel.RegisterAsync(CancellationToken.None);

        Assert.True(viewModel.CanContinue);
        Assert.Equal(4, viewModel.Results.Count);
        Assert.Contains(viewModel.Results, result => result.AccountHandle == "username.peer@mastodon.social");
        Assert.Contains(viewModel.Results, result => result.AccountHandle == "username.peer@pixelfed.social");
        Assert.Contains(viewModel.Results, result => result.AccountHandle == "username.peer@video.blender.org");
        Assert.Contains(viewModel.Results, result => result.AccountHandle == "username.peer@lemmy.world");
        Assert.All(viewModel.Results, result => Assert.Equal("Created", result.State));
    }

    [Fact]
    public async Task Registration_requires_email_and_matching_password()
    {
        var viewModel = new RegistrationViewModel(
            [new MockMastodonClient()],
            new DemoConfigProvider(),
            new EchoLocalizationService(),
            new AppSession())
        {
            AccountName = "username",
            Email = "user@example.com",
            Password = "",
            ConfirmPassword = ""
        };

        await viewModel.RegisterAsync(CancellationToken.None);

        Assert.False(viewModel.CanContinue);
        Assert.Equal("error.passwordTooShort", viewModel.Message);
        Assert.Empty(viewModel.Results);
    }

    private sealed class DemoConfigProvider : IInstanceConfigProvider
    {
        public Task<FediverseInstancesConfig> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(JsonInstanceConfigProvider.CreateDemoConfig());
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
