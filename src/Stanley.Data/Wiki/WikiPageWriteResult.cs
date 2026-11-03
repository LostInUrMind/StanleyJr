namespace Stanley.Data.Wiki;

/// <summary>
/// What a <c>/wiki-admin</c> write did, or the rule that refused it.
/// </summary>
public enum WikiPageWriteOutcome
{
    /// <summary>Nothing was refused: the write went through.</summary>
    Ok,

    /// <summary>The <c>page</c> option matched no wiki page.</summary>
    PageNotFound,

    /// <summary>
    /// The key normalizes to nothing — punctuation or symbols alone — so no <c>/wiki</c> input
    /// could ever reach the page. The rule a fighter's name and an alias follow too.
    /// </summary>
    KeyNotUsable,

    /// <summary>Another wiki page already answers to that key.</summary>
    KeyTaken,

    /// <summary>
    /// The key already finds a fighter, by its name or one of its aliases. Wiki pages are
    /// resolved before fighters, so the page would hide that fighter from <c>/wiki</c>.
    /// </summary>
    KeyFindsFighter,

    /// <summary>The URL was blank or whitespace.</summary>
    UrlMissing,

    /// <summary>The URL was not an absolute <c>http</c> or <c>https</c> address.</summary>
    UrlNotAUrl,

    /// <summary>An edit gave neither a new key nor a new URL, so there was nothing to do.</summary>
    NothingGiven,

    /// <summary>An edit gave only values the page already holds.</summary>
    NothingToChange,
}

/// <summary>
/// Which part of a wiki page a refusal or a confirmation is about, named structurally so the
/// data layer holds no wording.
/// </summary>
public enum WikiPageField
{
    Key,
    Url,
}

/// <summary>
/// One field an edit changed, before and after, for the private confirmation.
/// </summary>
/// <param name="Field">Which field changed.</param>
/// <param name="Before">Its stored value.</param>
/// <param name="After">Its new value.</param>
public sealed record WikiPageFieldChange(WikiPageField Field, string Before, string After);

/// <summary>
/// The outcome of <c>/wiki-admin add</c> or <c>/wiki-admin edit</c>, with whatever the private
/// confirmation has to show.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Key">
/// The key the write concerned: the page's key after the write, or the key that was refused.
/// Null when the outcome turns on no key.
/// </param>
/// <param name="Url">The page's URL after the write. Null for every refusal.</param>
/// <param name="Field">
/// The option a refusal is about, for the outcomes that turn on one value.
/// </param>
/// <param name="Detail">
/// The value the outcome turns on, as data: the page or the fighter that already holds the key,
/// or the text that was not a web address.
/// </param>
/// <param name="Changes">The fields an edit actually changed. Empty for every other outcome.</param>
public sealed record WikiPageWriteResult(
    WikiPageWriteOutcome Outcome,
    string? Key,
    string? Url,
    WikiPageField? Field,
    string? Detail,
    IReadOnlyList<WikiPageFieldChange> Changes)
{
    /// <summary>A page that did not exist before, so there is nothing to show but itself.</summary>
    public static WikiPageWriteResult Added(string key, string url) =>
        new(WikiPageWriteOutcome.Ok, key, url, null, null, []);

    /// <summary>A page that was changed, with the old and new values of what changed.</summary>
    public static WikiPageWriteResult Edited(
        string key,
        string url,
        IReadOnlyList<WikiPageFieldChange> changes) =>
        new(WikiPageWriteOutcome.Ok, key, url, null, null, changes);

    public static WikiPageWriteResult NotFound() =>
        new(WikiPageWriteOutcome.PageNotFound, null, null, null, null, []);

    public static WikiPageWriteResult KeyNotUsable(string key) =>
        new(WikiPageWriteOutcome.KeyNotUsable, key, null, WikiPageField.Key, null, []);

    /// <param name="key">The key that was refused.</param>
    /// <param name="owner">The key of the page that already answers to it.</param>
    public static WikiPageWriteResult KeyTaken(string key, string owner) =>
        new(WikiPageWriteOutcome.KeyTaken, key, null, WikiPageField.Key, owner, []);

    /// <param name="key">The key that was refused.</param>
    /// <param name="fighterName">The fighter it already finds.</param>
    public static WikiPageWriteResult KeyFindsFighter(string key, string fighterName) =>
        new(WikiPageWriteOutcome.KeyFindsFighter, key, null, WikiPageField.Key, fighterName, []);

    public static WikiPageWriteResult UrlMissing() =>
        new(WikiPageWriteOutcome.UrlMissing, null, null, WikiPageField.Url, null, []);

    public static WikiPageWriteResult NotAUrl(string given) =>
        new(WikiPageWriteOutcome.UrlNotAUrl, null, null, WikiPageField.Url, given, []);

    public static WikiPageWriteResult NothingGiven() =>
        new(WikiPageWriteOutcome.NothingGiven, null, null, null, null, []);

    public static WikiPageWriteResult NothingToChange(string key) =>
        new(WikiPageWriteOutcome.NothingToChange, key, null, null, null, []);
}
