using System.Globalization;
using System.Text.RegularExpressions;

namespace SchoolManager.Helpers;

/// <summary>
/// Formato visual de nombres de personas (no modifica datos almacenados).
/// </summary>
public static class DisplayNameHelper
{
    private static readonly HashSet<string> LowercaseParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "del", "la", "las", "los", "y", "e", "da", "do", "das", "dos", "van", "von", "mc", "mac"
    };

    private static readonly Regex CodeLikePattern = new(
        @"^[A-Z0-9]{2,}[-_/][A-Z0-9][A-Z0-9\-_/]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool LooksLikeEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@', StringComparison.Ordinal);

    public static bool ShouldFormatAsPersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (LooksLikeEmail(trimmed)) return false;
        if (CodeLikePattern.IsMatch(trimmed)) return false;
        return true;
    }

    public static string ToDisplayPersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        if (!ShouldFormatAsPersonName(trimmed)) return trimmed;
        return ToTitleCaseWords(trimmed);
    }

    public static string FormatFullName(string? firstName, string? lastName)
    {
        var parts = new[]
        {
            ToDisplayPersonName(firstName),
            ToDisplayPersonName(lastName)
        }.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(" ", parts);
    }

    public static string FormatFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var trimmed = fullName.Trim();
        if (!ShouldFormatAsPersonName(trimmed)) return trimmed;

        if (trimmed.Contains(',', StringComparison.Ordinal))
        {
            var segments = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
                return $"{ToDisplayPersonName(segments[0])}, {ToDisplayPersonName(segments[1])}";
        }

        return ToTitleCaseWords(trimmed);
    }

    private static string ToTitleCaseWords(string text)
    {
        var culture = CultureInfo.CurrentCulture;
        var lower = text.ToLower(culture);
        var titled = culture.TextInfo.ToTitleCase(lower);
        var words = titled.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < words.Length; i++)
        {
            if (LowercaseParticles.Contains(words[i]))
                words[i] = words[i].ToLower(culture);
        }
        return string.Join(" ", words);
    }
}
