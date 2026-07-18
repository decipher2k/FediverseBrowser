using System.Globalization;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Core.Services;
using FediverseHub.Core.ViewModels;
using FediverseHub.Infrastructure.Mock;
using FediverseHub.Infrastructure.Persistence;

namespace FediverseHub.Tests;

public sealed class HashtagAndInterestTests
{
    [Fact]
    public void Interest_catalog_contains_required_twenty_categories()
    {
        var catalog = new InterestCatalog();

        var interests = catalog.GetAll();

        Assert.Equal(20, interests.Count);
        Assert.Equal("#art", interests[0].Hashtags[0]);
        Assert.Equal("#fediverse", interests[^1].Hashtags[0]);
    }

    [Fact]
    public void Interest_catalog_maps_selected_categories_to_normalized_unique_hashtags()
    {
        var catalog = new InterestCatalog();

        var hashtags = catalog.GetHashtagsForInterests(["technology", "programming"]);

        Assert.Contains("#technology", hashtags);
        Assert.Contains("#opensource", hashtags);
        Assert.Contains("#dotnet", hashtags);
        Assert.Equal(hashtags.Count, hashtags.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("Art", "#art")]
    [InlineData("#OpenSource", "#opensource")]
    [InlineData("  #3dprinting  ", "#3dprinting")]
    [InlineData("bad tag!", "")]
    public void Hashtag_normalizer_validates_and_normalizes_user_input(string input, string expected)
    {
        Assert.Equal(expected, HashtagNormalizer.Normalize(input));
    }

    [Fact]
    public async Task Onboarding_selected_interests_are_saved_as_followed_hashtags()
    {
        var settingsStore = new InMemorySettingsStore();
        var followService = new HashtagFollowService(
            settingsStore,
            [new MockMastodonClient(), new MockPixelfedClient()]);
        var viewModel = new OnboardingViewModel(
            new InterestCatalog(),
            followService,
            new EchoLocalizationService());

        viewModel.Interests[0].IsSelected = true;
        viewModel.CustomHashtags = "Maker, #OpenSource";

        await viewModel.CompleteAsync(CancellationToken.None);

        var settings = await settingsStore.LoadAsync(CancellationToken.None);
        Assert.Null(viewModel.ValidationMessage);
        Assert.Contains("#art", settings.FollowedHashtags);
        Assert.Contains("#kunst", settings.FollowedHashtags);
        Assert.Contains("#culture", settings.FollowedHashtags);
        Assert.Contains("#maker", settings.FollowedHashtags);
        Assert.Contains("#opensource", settings.FollowedHashtags);
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
