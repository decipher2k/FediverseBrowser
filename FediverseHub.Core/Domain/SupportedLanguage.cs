using System.Globalization;

namespace FediverseHub.Core.Domain;

public sealed class SupportedLanguage
{
    public required string Code { get; init; }
    public required string NativeName { get; init; }
    public required string EnglishName { get; init; }

    public CultureInfo Culture => CultureInfo.GetCultureInfo(Code);
}
