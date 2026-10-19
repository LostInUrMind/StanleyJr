using Stanley.Core.Naming;

namespace Stanley.Core.Wiki;

/// <summary>
/// A wiki page that is not a fighter, and so has no database row to look up (FR-08).
/// </summary>
public sealed class WikiExtraPage
{
    /// <param name="title">The wiki page title, which is also what suggestions display.</param>
    /// <param name="synonyms">
    /// Shorter names users type for it. The title itself is always a key and does not need
    /// repeating here.
    /// </param>
    public WikiExtraPage(string title, params string[] synonyms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(synonyms);

        Title = title;
        Url = WikiPages.UrlForTitle(title);
        Slug = NameNormalizer.Normalize(title);
        Keys = [Slug, .. synonyms.Select(NameNormalizer.Normalize)];
    }

    public string Title { get; }

    public string Url { get; }

    public string Slug { get; }

    /// <summary>Every normalized name this page answers to, including <see cref="Slug"/>.</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>Whether this page is exactly what <paramref name="slug"/> names.</summary>
    public bool Matches(string slug) => Keys.Contains(slug, StringComparer.Ordinal);

    /// <summary>Whether this page is worth offering for a partly typed <paramref name="slug"/>.</summary>
    public bool Suggests(string slug) =>
        Keys.Any(key => key.Contains(slug, StringComparison.Ordinal));
}
