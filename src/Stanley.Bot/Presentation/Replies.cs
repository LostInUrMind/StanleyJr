using Stanley.Core.Wiki;
using Stanley.Data.Aliases;
using Stanley.Data.Lookup;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Presentation;

/// <summary>
/// Wording that more than one command shows, so the text is corrected in one place.
/// </summary>
internal static class Replies
{
    /// <summary>The longest text Discord will display for one autocomplete choice.</summary>
    public const int MaxChoiceLength = 100;

    /// <summary>The longest message Discord will accept.</summary>
    public const int MaxMessageLength = 2000;

    /// <summary>
    /// The credits: who made Stanley.
    /// </summary>
    public static string Credits(string? icon)
    {
        string developed = string.IsNullOrWhiteSpace(icon)
            ? "Developed by The SGM Wiki Team"
            : $"Developed by {icon} The SGM Wiki Team";

        return $"{developed}\nArt by @switch5721 on Discord";
    }

    /// <summary>
    /// The reply to an interaction for a command Stanley no longer has.
    /// </summary>
    public const string CommandGone =
        "That command does not exist any more. Discord sometimes keeps offering an old one for "
        + "a few minutes — type / again to see the current commands.";

    /// <summary>
    /// The reply when nothing matched.
    /// </summary>
    public static string NoMatch(string input) =>
        $"No fighter matches ‘{input}’. Try the suggestions while typing.";

    /// <summary>
    /// The same reply for <c>/wiki</c>, which answers for the wiki pages that are not fighters
    /// as well as for fighters, so "no fighter" would not be the whole truth.
    /// </summary>
    public static string NoWikiMatch(string input) =>
        $"No fighter or wiki page matches ‘{input}’. Try the suggestions while typing.";

    /// <summary>
    /// What an autocomplete choice reads as. An alias shows the fighter it belongs to, requires a typed alias to suggest.
    /// </summary>
    public static string ChoiceLabel(FighterSuggestion suggestion)
    {
        string text = suggestion.IsAlias
            ? $"{suggestion.Label} → {suggestion.FighterName}"
            : suggestion.Label;

        return Truncate(text, MaxChoiceLength);
    }

    /// <summary>
    /// What a choice reads as in <c>/alias-admin remove</c>, where every choice is an alias and the
    /// fighter is what tells two similar ones apart.
    /// </summary>
    public static string ChoiceLabel(AliasSuggestion suggestion) =>
        Truncate($"{suggestion.Alias} → {suggestion.FighterName}", MaxChoiceLength);

    /// <summary>
    /// What a choice reads as in <c>/wiki-admin edit</c> and <c>/wiki-admin remove</c>, where
    /// every choice is a wiki page and the page it points at is what tells two keys apart.
    /// </summary>
    /// <remarks>
    /// The wiki's own prefix is dropped, so the part that differs is the part shown: two keys
    /// for one page read alike, and a key pointing somewhere else reads as that address.
    /// </remarks>
    public static string ChoiceLabel(WikiPageSuggestion suggestion)
    {
        string page =
            suggestion.Url.StartsWith(WikiUrls.BaseUrl, StringComparison.OrdinalIgnoreCase)
                ? suggestion.Url[WikiUrls.BaseUrl.Length..]
                : suggestion.Url;

        return Truncate($"{suggestion.Key} → {page}", MaxChoiceLength);
    }

    /// <summary>
    /// A fighter's other names, including the case of a fighter that has none.
    /// </summary>
    /// <remarks>
    /// Nothing limits how many aliases a fighter may have, and a sentence listing all of them
    /// could outgrow a Discord message, which would fail the command rather than answer it. So
    /// a list that long ends in a count of the rest, the same choice the card makes for a
    /// fighter with more abilities than one message holds.
    /// </remarks>
    public static string AliasList(string fighterName, IReadOnlyList<string> aliases)
    {
        if (aliases.Count == 0)
        {
            return $"{fighterName} has no aliases.";
        }

        string sentence = $"{fighterName} is also known as {AndList(aliases)}.";

        if (sentence.Length <= MaxMessageLength)
        {
            return sentence;
        }

        int kept = aliases.Count;

        do
        {
            kept--;
            sentence = $"{fighterName} is also known as {AndList(aliases.Take(kept).ToList())}, and {aliases.Count - kept} more.";
        }
        while (kept > 1 && sentence.Length > MaxMessageLength);

        // One alias long enough to fill a message on its own cannot come from /alias-admin add,
        // which caps the option at 100 characters, but the reply is cut to fit either way.
        return Truncate(sentence, MaxMessageLength);
    }

    /// <summary>The private confirmation after <c>/alias-admin add</c>.</summary>
    public static string AliasAdded(string alias, string fighterName) =>
        $"Added ‘{alias}’ as another name for {fighterName}.";

    /// <summary>
    /// The refusal when the alias is already in use, naming the fighter that uses it.
    /// </summary>
    public static string AliasTaken(string alias, string fighterName) =>
        $"‘{alias}’ is already an alias of {fighterName}.";

    /// <summary>The same refusal for an alias that is some fighter's own name.</summary>
    public static string AliasIsFighterName(string alias, string fighterName) =>
        $"‘{alias}’ is {fighterName}’s own name, so it already finds that fighter.";

    /// <summary>
    /// The refusal for an alias of punctuation or symbols alone.
    /// </summary>
    public static string AliasNotUsable(string alias) =>
        $"‘{alias}’ has no letters or digits, so nothing would ever match it. Use an alias with at least one letter or digit.";

    /// <summary>The private confirmation after <c>/alias-admin remove</c>.</summary>
    public static string AliasRemoved(string alias, string fighterName) =>
        $"Removed the alias ‘{alias}’ from {fighterName}.";

    /// <summary>
    /// The reply when the alias to remove does not exist.
    /// </summary>
    public static string NoAliasMatch(string input) =>
        $"No alias matches ‘{input}’. Try the suggestions while typing.";

    private static string AndList(IReadOnlyList<string> items) => items.Count switch
    {
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[^1]}",
    };

    private static string Truncate(string text, int limit) =>
        text.Length <= limit ? text : text[..limit];
}
