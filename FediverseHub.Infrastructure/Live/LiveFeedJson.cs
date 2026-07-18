using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FediverseHub.Infrastructure.Live;

internal static partial class LiveFeedJson
{
    public static string? String(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    public static int Int32(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.TryGetInt32(out var value)
            ? value
            : 0;

    public static JsonElement? Object(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.Object
            ? property
            : null;

    public static JsonElement? Array(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.Array
            ? property
            : null;

    public static DateTimeOffset Date(string? value)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    public static string TextFromHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withBreaks = BreakRegex().Replace(value, "\n");
        var withoutTags = TagRegex().Replace(withBreaks, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    public static Uri BaseUri(string baseUrl)
    {
        var normalized = string.IsNullOrWhiteSpace(baseUrl)
            ? "https://mastodon.social"
            : baseUrl.Trim();

        if (!normalized.EndsWith('/'))
        {
            normalized += "/";
        }

        return new Uri(normalized, UriKind.Absolute);
    }

    public static string AbsoluteUrl(Uri baseUri, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return baseUri.ToString();
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : new Uri(baseUri, value.TrimStart('/')).ToString();
    }

    public static IReadOnlyList<string> ExtractHashtags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return System.Array.Empty<string>();
        }

        return HashtagRegex()
            .Matches(value)
            .Select(match => "#" + match.Groups[1].Value.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [GeneratedRegex("<br\\s*/?>|</p>|</div>|</li>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BreakRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"(?<!\w)#([A-Za-z0-9_]{2,64})", RegexOptions.CultureInvariant)]
    private static partial Regex HashtagRegex();
}
