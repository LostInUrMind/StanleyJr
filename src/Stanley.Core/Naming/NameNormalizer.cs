using System.Globalization;
using System.Text;

namespace Stanley.Core.Naming;

/// <summary>
/// Turns a display name into the slug used for lookups (FR-02).
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // FormD splits "è" into "e" plus a combining accent, so the accent can be dropped
        // and "Très Chic" becomes "treschic" whether or not the user typed the accent.
        string decomposed = name.Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
        }

        return slug.ToString();
    }

    public static bool IsNormalizable(string? name) => Normalize(name).Length > 0;
}
