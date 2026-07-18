using System.Globalization;
using FediverseHub.Core.Domain;

namespace FediverseHub.Core.Interfaces;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }

    IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    string GetString(string key);

    Task SetLanguageAsync(string languageCode, CancellationToken cancellationToken);

    Task UseSystemLanguageAsync(CancellationToken cancellationToken);
}
