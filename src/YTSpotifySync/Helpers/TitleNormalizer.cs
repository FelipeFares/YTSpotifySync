using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace YTSpotifySync.Helpers;

public static partial class TitleNormalizer
{
    [GeneratedRegex(@"[^\p{L}\p{Nd}\s]")]
    private static partial Regex NonLetterOrDigitRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();

    /// <summary>
    /// Normalizes a video or episode title for reliable cross-platform comparison.
    /// Converts to lowercase, strips accents/diacritics, removes punctuation and non-alphanumeric chars,
    /// and collapses whitespace.
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. Lowercase
        string text = input.Trim().ToLowerInvariant();

        // 2. Decompose diacritics
        string normalizedFormD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalizedFormD)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string withoutAccents = sb.ToString().Normalize(NormalizationForm.FormC);

        // 3. Remove non-letter/non-digit characters except whitespace
        string cleaned = NonLetterOrDigitRegex().Replace(withoutAccents, " ");

        // 4. Collapse multiple spaces and trim
        string result = MultipleWhitespaceRegex().Replace(cleaned, " ").Trim();

        return result;
    }
}
