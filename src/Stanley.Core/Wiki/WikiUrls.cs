namespace Stanley.Core.Wiki;

/// <summary>
/// Builds the wiki URL of a fighter's page.
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
/// <para>
/// Only fighters are built this way. The wiki pages that are not fighters are rows in
/// <c>wiki_pages</c> holding a whole URL each, so an editor can add one without a deploy; they
/// were a hardcoded list here until then.
/// </para>
/// </remarks>
public static class WikiUrls
{
    public const string BaseUrl = "https://skullgirlsmobile.fandom.com/wiki/";

    /// <summary>The URL of a page with the given title.</summary>
    public static string UrlForTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return BaseUrl + title.Replace(' ', '_');
    }

    public static string UrlFor(string title, string? storedUrl) =>
        string.IsNullOrWhiteSpace(storedUrl) ? UrlForTitle(title) : storedUrl;
}
