using System.Globalization;
using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;
using FediverseHub.Infrastructure.Localization;
using FediverseHub.Infrastructure.Persistence;

namespace FediverseHub.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void Uses_supported_system_language_on_first_start()
    {
        var service = CreateService(new CultureInfo("de-DE"));

        Assert.Equal("de", service.CurrentCulture.TwoLetterISOLanguageName);
        Assert.Equal("Alle", service.GetString("tab.all"));
    }

    [Fact]
    public void Falls_back_to_english_for_unsupported_system_language()
    {
        var service = CreateService(new CultureInfo("nl-NL"));

        Assert.Equal("en", service.CurrentCulture.TwoLetterISOLanguageName);
        Assert.Equal("All", service.GetString("tab.all"));
    }

    [Fact]
    public async Task Manual_language_choice_is_persisted_and_reused()
    {
        var store = new InMemorySettingsStore();
        var service = CreateService(new CultureInfo("de-DE"), store);

        await service.SetLanguageAsync("ja", CancellationToken.None);
        var settings = await store.LoadAsync(CancellationToken.None);
        var restarted = CreateService(new CultureInfo("de-DE"), store);

        Assert.Equal("ja", settings.LanguageCode);
        Assert.False(settings.UseSystemLanguage);
        Assert.Equal("ja", restarted.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public async Task Every_supported_language_contains_all_english_keys()
    {
        var resources = ResourcesPath();
        var english = await ReadAsync(Path.Combine(resources, "Strings.en.json"));

        foreach (var file in Directory.EnumerateFiles(resources, "Strings.*.json"))
        {
            var values = await ReadAsync(file);
            var missing = english.Keys.Except(values.Keys, StringComparer.Ordinal).ToArray();
            Assert.True(missing.Length == 0, $"{Path.GetFileName(file)} misses: {string.Join(", ", missing)}");
        }
    }

    [Fact]
    public void Supported_languages_match_required_codes()
    {
        var service = CreateService(new CultureInfo("en-US"));

        Assert.Equal(
            ["en", "de", "fr", "es", "it", "hi", "ja", "zh", "ru"],
            service.SupportedLanguages.Select(language => language.Code));
        Assert.Contains(service.SupportedLanguages, language => language.NativeName == "Deutsch");
        Assert.Contains(service.SupportedLanguages, language => language.NativeName == "日本語");
    }

    private static JsonLocalizationService CreateService(
        CultureInfo culture,
        ISettingsStore? settingsStore = null) =>
        new(
            settingsStore ?? new InMemorySettingsStore(new AppSettings { UseSystemLanguage = true }),
            new FakeSystemCultureProvider(culture),
            ResourcesPath());

    private static string ResourcesPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Localization", "Resources");
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FediverseHub.Infrastructure", "Localization", "Resources"));
    }

    private static async Task<Dictionary<string, string>> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream)
            ?? new Dictionary<string, string>();
    }

    private sealed class FakeSystemCultureProvider(CultureInfo culture) : ISystemCultureProvider
    {
        public CultureInfo GetSystemCulture() => culture;
    }
}
