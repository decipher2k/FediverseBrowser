using System.Text.RegularExpressions;

namespace FediverseHub.Core.Services;

public static partial class HashtagNormalizer
{
    public static string Normalize(string hashtag)
    {
        var trimmed = hashtag.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var withoutHash = trimmed.TrimStart('#').Trim();
        if (!HashtagRegex().IsMatch(withoutHash))
        {
            return string.Empty;
        }

        return "#" + withoutHash.ToLowerInvariant();
    }

    public static IReadOnlyList<string> NormalizeMany(IEnumerable<string> hashtags) =>
        hashtags
            .Select(Normalize)
            .Where(static tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    [GeneratedRegex(@"^[\p{L}\p{N}_][\p{L}\p{N}_-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashtagRegex();
}
