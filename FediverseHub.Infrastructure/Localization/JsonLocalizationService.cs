using System.Globalization;
using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Localization;

public sealed class JsonLocalizationService : ILocalizationService
{
    private static readonly IReadOnlyList<SupportedLanguage> Languages =
    [
        new() { Code = "en", NativeName = "English", EnglishName = "English" },
        new() { Code = "de", NativeName = "Deutsch", EnglishName = "German" },
        new() { Code = "fr", NativeName = "Français", EnglishName = "French" },
        new() { Code = "es", NativeName = "Español", EnglishName = "Spanish" },
        new() { Code = "it", NativeName = "Italiano", EnglishName = "Italian" },
        new() { Code = "hi", NativeName = "हिन्दी", EnglishName = "Hindi" },
        new() { Code = "ja", NativeName = "日本語", EnglishName = "Japanese" },
        new() { Code = "zh", NativeName = "中文", EnglishName = "Chinese" },
        new() { Code = "ru", NativeName = "Русский", EnglishName = "Russian" }
    ];

    private readonly ISettingsStore _settingsStore;
    private readonly ISystemCultureProvider _systemCultureProvider;
    private readonly Dictionary<string, Dictionary<string, string>> _resources;

    public JsonLocalizationService(
        ISettingsStore settingsStore,
        ISystemCultureProvider systemCultureProvider,
        string? resourcesDirectory = null)
    {
        _settingsStore = settingsStore;
        _systemCultureProvider = systemCultureProvider;
        _resources = LoadResources(resourcesDirectory ?? Path.Combine(AppContext.BaseDirectory, "Localization", "Resources"));

        var settings = _settingsStore.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        CurrentCulture = settings.UseSystemLanguage || string.IsNullOrWhiteSpace(settings.LanguageCode)
            ? ResolveSupportedCulture(_systemCultureProvider.GetSystemCulture())
            : ResolveSupportedCulture(CultureInfo.GetCultureInfo(settings.LanguageCode));
    }

    public CultureInfo CurrentCulture { get; private set; }

    public IReadOnlyList<SupportedLanguage> SupportedLanguages => Languages;

    public string GetString(string key)
    {
        var language = CurrentCulture.TwoLetterISOLanguageName;
        if (_resources.TryGetValue(language, out var local) && local.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_resources.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public async Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken)
    {
        CurrentCulture = ResolveSupportedCulture(CultureInfo.GetCultureInfo(languageCode));
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;

        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        await _settingsStore.SaveAsync(
            settings with { LanguageCode = CurrentCulture.TwoLetterISOLanguageName, UseSystemLanguage = false },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UseSystemLanguageAsync(CancellationToken cancellationToken)
    {
        CurrentCulture = ResolveSupportedCulture(_systemCultureProvider.GetSystemCulture());
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;

        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        await _settingsStore.SaveAsync(
            settings with { LanguageCode = null, UseSystemLanguage = true },
            cancellationToken).ConfigureAwait(false);
    }

    private static CultureInfo ResolveSupportedCulture(CultureInfo culture)
    {
        var languageCode = culture.TwoLetterISOLanguageName;
        var supported = Languages.Any(language => language.Code == languageCode) ? languageCode : "en";
        return CultureInfo.GetCultureInfo(supported);
    }

    private static Dictionary<string, Dictionary<string, string>> LoadResources(string resourcesDirectory)
    {
        var resources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in Languages)
        {
            var fileName = Path.Combine(resourcesDirectory, $"Strings.{language.Code}.json");
            if (!File.Exists(fileName))
            {
                continue;
            }

            var json = File.ReadAllText(fileName);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
            resources[language.Code] = values;
        }

        return resources;
    }
}
