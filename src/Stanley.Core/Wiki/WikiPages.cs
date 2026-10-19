using Stanley.Core.Naming;

namespace Stanley.Core.Wiki;

/// <summary>
/// Builds wiki URLs and holds the handful of wiki pages that are not fighters.
/// </summary>
/// <remarks>
/// <para>
/// The URL is built from a page title rather than stored, because the wiki's titles are the
/// fighters' names: <c>Vintage Virtuoso</c> is at <c>/wiki/Vintage_Virtuoso</c>. A fighter
/// whose page title does not follow that rule is the reason <c>variants.wiki_url</c> exists,
/// and <see cref="UrlFor(string, string?)"/> is where that override wins.
/// </para>
/// <para>
/// Spaces become underscores and nothing else is escaped, which is what v1 sent and what the
/// FRD's acceptance criteria state. Apostrophes are left alone: <c>Fortune's_Favor</c> is a
/// working URL.
/// </para>
/// </remarks>
public static class WikiPages
{
    public const string BaseUrl = "https://skullgirlsmobile.fandom.com/wiki/";

    /// <summary>
    /// The wiki pages that are not fighters. A short list in code, next to the URL
    /// builder, because these are wiki facts rather than fighter data: nothing an editor would
    /// maintain in the database (migration plan section 3).
    /// </summary>
    public static IReadOnlyList<WikiExtraPage> Extras { get; } =
    [
        // v1 answered both "critless" and the full title.
        new("Critless Skill Tree", "critless"),
        new("Fortune's Favor"),
    ];

    /// <summary>The URL of a page with the given title.</summary>
    public static string UrlForTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return BaseUrl + title.Replace(' ', '_');
    }

    public static string UrlFor(string title, string? storedUrl) =>
        string.IsNullOrWhiteSpace(storedUrl) ? UrlForTitle(title) : storedUrl;

    /// <summary>
    /// The non-fighter page <paramref name="input"/> names, or <see langword="null"/>.
    /// </summary>
    public static WikiExtraPage? Find(string? input)
    {
        string slug = NameNormalizer.Normalize(input);

        return slug.Length == 0
            ? null
            : Extras.FirstOrDefault(page => page.Matches(slug));
    }

    /// <summary>
    /// The non-fighter pages worth suggesting for what has been typed so far (FR-08).
    /// </summary>
    public static IEnumerable<WikiExtraPage> Suggest(string? input)
    {
        string slug = NameNormalizer.Normalize(input);

        return slug.Length == 0
            ? Extras
            : Extras.Where(page => page.Suggests(slug));
    }
}
