using System.Globalization;
using Stanley.Data.Fighters;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Presentation;

/// <summary>
/// The wording of the editor commands.
/// </summary>
/// <remarks>
/// Separate from <see cref="Replies"/>, which holds what everyone sees, because this is shown
/// to editors alone and is read by both the <c>/fighter-admin</c> command and the buttons and
/// forms it opens.
/// </remarks>
internal static class AdminReplies
{
    /// <summary>The longest title Discord shows on a pop-up form.</summary>
    public const int MaxModalTitleLength = 45;

    /// <summary>After cancelling, nothing is left to say but that nothing happened.</summary>
    public const string Cancelled = "Cancelled. Nothing was changed.";

    /// <summary>
    /// When a button or a form comes back for a fighter that has since been removed. The
    /// editor cannot be shown a name, because there is no row left to read one from.
    /// </summary>
    public const string StaleFighter = "That fighter no longer exists, so nothing was changed.";

    /// <summary>The same, for a wiki page an edit came back for after it was removed.</summary>
    private const string StaleWikiPage =
        "That wiki page no longer exists, so nothing was changed.";

    /// <summary>
    /// <c>remove all</c> takes the whole block, so an SA number with it would be a number the
    /// command cannot honour rather than a narrower removal.
    /// </summary>
    public const string SaNumberNeedsSet =
        "The SA number belongs to ‘set’. Removing abilities removes every one the fighter has, so there is no SA number to give.";

    /// <summary>How much of one value a change line shows before it is cut.</summary>
    private const int MaxValueLength = 120;

    /// <summary>What a field that holds nothing reads as in a change line.</summary>
    private const string NotSet = "(not set)";

    /// <summary>The confirmation or refusal after <c>/fighter-admin add</c>.</summary>
    public static string FighterAdded(FighterWriteResult result, string input)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome is FighterWriteOutcome.Ok
            ? $"Added {result.FighterName}. Give it its signature abilities with `/fighter-admin ability`, starting at SA1."
            : Refused(result, input);
    }

    /// <summary>
    /// The confirmation or refusal after <c>/fighter-admin edit</c>. 
    /// </summary>
    public static string FighterEdited(FighterWriteResult result, string input)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome is FighterWriteOutcome.Ok
            ? Changes(result.FighterName!, result.Changes)
            : Refused(result, input);
    }

    /// <summary>
    /// The refusal when a form cannot be opened for the ability the editor asked for.
    /// </summary>
    public static string AbilityNotPrompted(AbilityPrompt prompt, string input)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        return prompt.Outcome switch
        {
            FighterWriteOutcome.AbilityNumberOutOfRange =>
                AbilityRange(prompt.FighterName!, prompt.AbilityCount),
            _ => Replies.NoMatch(input),
        };
    }

    /// <summary>
    /// The title of the form for one ability. Short: Discord shows at most
    /// <see cref="MaxModalTitleLength"/> characters, and the fighter's name is the part worth
    /// keeping.
    /// </summary>
    public static string AbilityModalTitle(AbilityPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        return Truncate($"SA{N(prompt.Ordinal)} · {prompt.FighterName}", MaxModalTitleLength);
    }

    /// <summary>The confirmation or refusal after a form is submitted.</summary>
    public static string AbilityWritten(AbilitySetResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            FighterWriteOutcome.Ok when result.WasAdded =>
                $"Added SA{N(result.Ordinal)} to {result.FighterName}.",
            FighterWriteOutcome.Ok =>
                $"Rewrote {result.FighterName}’s SA{N(result.Ordinal)}.",
            FighterWriteOutcome.AbilityNumberOutOfRange =>
                AbilityRange(result.FighterName!, result.AbilityCount),
            FighterWriteOutcome.ValueMissing => Blank(FighterField.AbilityText),
            FighterWriteOutcome.AbilityTextTooLong => TooLong(),
            _ => StaleFighter,
        };
    }

    /// <summary>
    /// The question the confirm and cancel buttons answer before a fighter's abilities are
    /// removed (FRD section 6.1).
    /// </summary>
    public static string ConfirmRemoveAbilities(FighterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return $"Remove all {Abilities(target.AbilityCount)} of {target.Name}? "
            + "Abilities go as a block — there is no removing one on its own — and you then enter them again from SA1.";
    }

    /// <summary>The confirmation or refusal after that block is removed.</summary>
    public static string AbilitiesRemoved(AbilityRemovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            FighterWriteOutcome.Ok =>
                $"Removed all {Abilities(result.RemovedCount)} of {result.FighterName}. "
                + "Enter them again from SA1 with `/fighter-admin ability`.",
            FighterWriteOutcome.NoAbilities => NoAbilities(result.FighterName!),
            _ => StaleFighter,
        };
    }

    public static string NoAbilities(string fighterName) =>
        $"{fighterName} has no signature abilities to remove.";

    /// <summary>
    /// The question the confirm and cancel buttons answer before a fighter is removed, naming
    /// what goes with it.
    /// </summary>
    public static string ConfirmRemoveFighter(FighterTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return $"Remove {target.Name}? Its {Abilities(target.AbilityCount)} and "
            + $"{Aliases(target.AliasCount)} go with it. Stanley records the removal, but "
            + "nothing in Discord puts it back.";
    }

    /// <summary>The confirmation or refusal after a fighter is removed.</summary>
    public static string FighterRemoved(FighterRemovalResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome is FighterWriteOutcome.Ok
            ? $"Removed {result.FighterName}, with its "
                + $"{Abilities(result.RemovedAbilities)} and "
                + $"{Aliases(result.RemovedAliases)}."
            : StaleFighter;
    }

    /// <summary>The confirmation or refusal after <c>/wiki-admin add</c>.</summary>
    public static string WikiPageAdded(WikiPageWriteResult result, string input)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome is WikiPageWriteOutcome.Ok
            ? $"Added the wiki page ‘{result.Key}’, linking {result.Url}."
            : WikiRefused(result, input);
    }

    /// <summary>The confirmation or refusal after <c>/wiki-admin edit</c>.</summary>
    public static string WikiPageEdited(WikiPageWriteResult result, string input)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome is WikiPageWriteOutcome.Ok
            ? WikiChanges(result.Key!, result.Changes)
            : WikiRefused(result, input);
    }

    /// <summary>The confirmation after <c>/wiki-admin remove</c>.</summary>
    public static string WikiPageRemoved(RemovedWikiPage removed)
    {
        ArgumentNullException.ThrowIfNull(removed);

        return $"Removed the wiki page ‘{removed.Key}’, which linked {removed.Url}.";
    }

    /// <summary>
    /// The reply when the page to change or remove does not exist. Separate from
    /// <see cref="Replies.NoMatch"/>, which is about fighters.
    /// </summary>
    public static string NoWikiPageMatch(string input) =>
        $"No wiki page matches ‘{input}’. Try the suggestions while typing.";

    /// <summary>
    /// Every refusal of a write to a wiki page, each stating the rule it broke.
    /// </summary>
    private static string WikiRefused(WikiPageWriteResult result, string input) =>
        result.Outcome switch
        {
            WikiPageWriteOutcome.PageNotFound => NoWikiPageMatch(input),

            // The same rule as a fighter's name and an alias: matching ignores everything but
            // letters and digits, so a key of punctuation alone could never be typed back.
            WikiPageWriteOutcome.KeyNotUsable =>
                $"‘{result.Key}’ has no letters or digits, so nothing would ever match "
                + "it. Use a key with at least one letter or digit.",

            WikiPageWriteOutcome.KeyTaken =>
                $"‘{result.Key}’ already finds the wiki page ‘{result.Detail}’. "
                + "Change that page instead, or pick another key.",

            // Wiki pages are resolved before fighters, so this key would make that fighter
            // unreachable through /wiki.
            WikiPageWriteOutcome.KeyFindsFighter =>
                $"‘{result.Key}’ already finds the fighter {result.Detail}, so a wiki "
                + "page under that key would hide it. Use a different key.",

            WikiPageWriteOutcome.UrlMissing => $"The {WikiLabel(result.Field)} cannot be blank.",

            WikiPageWriteOutcome.UrlNotAUrl =>
                $"The {WikiLabel(result.Field)} has to be a web address starting with http:// "
                + $"or https://. ‘{result.Detail}’ is not one.",

            WikiPageWriteOutcome.NothingGiven =>
                "Give a new key, a new address, or both. Nothing was changed.",

            WikiPageWriteOutcome.NothingToChange =>
                $"Nothing to change: the wiki page ‘{result.Key}’ already holds every "
                + "value you gave.",

            _ => StaleWikiPage,
        };

    /// <summary>
    /// The old and new values of everything an edit changed, one per line, as the fighter
    /// confirmations show them.
    /// </summary>
    /// <remarks>
    /// A page has two fields, both capped by the command's options, so the message cannot
    /// outgrow what Discord accepts the way a fighter's can. It is cut to fit regardless,
    /// because a reply too long fails the command rather than answering it.
    /// </remarks>
    private static string WikiChanges(string key, IReadOnlyList<WikiPageFieldChange> changes)
    {
        string message = string.Join(
            '\n',
            changes
                .Select(change =>
                    $"• {WikiLabel(change.Field)}: {Truncate(change.Before, MaxValueLength)}"
                    + $" → {Truncate(change.After, MaxValueLength)}")
                .Prepend($"Updated the wiki page ‘{key}’:"));

        return Truncate(message, Replies.MaxMessageLength);
    }

    private static string WikiLabel(WikiPageField? field) => field switch
    {
        WikiPageField.Key => "key",
        WikiPageField.Url => "address",
        _ => "value",
    };

    /// <summary>
    /// Every refusal of a write to a fighter's own fields, each stating the rule it broke.
    /// </summary>
    private static string Refused(FighterWriteResult result, string input) => result.Outcome switch
    {
        FighterWriteOutcome.FighterNotFound => Replies.NoMatch(input),

        FighterWriteOutcome.NameNotUsable =>
            $"‘{result.FighterName}’ has no letters or digits, so nothing would ever match it. "
            + "Give the fighter a name with at least one letter or digit.",

        // The FRD words this for aliases; a fighter's name follows the same rule, because the
        // two are looked up together (database design section 3.2).
        FighterWriteOutcome.NameTaken =>
            $"‘{result.FighterName}’ already finds {result.Detail}. A fighter's name cannot be "
            + "another fighter's name or alias.",

        FighterWriteOutcome.ValueMissing => Blank(result.Field),

        FighterWriteOutcome.ValueNotAllowed => NotAllowed(result.Field, result.Detail),

        FighterWriteOutcome.ValueNotPositive =>
            $"The {Label(result.Field)} has to be a whole number greater than zero.",

        FighterWriteOutcome.ValueNotAUrl =>
            $"The {Label(result.Field)} has to be a web address starting with http:// or "
            + $"https://. ‘{result.Detail}’ is not one.",

        FighterWriteOutcome.NothingToChange =>
            $"Nothing to change: {result.FighterName} already holds every value you gave.",

        FighterWriteOutcome.AbilityTextTooLong => TooLong(),

        _ => StaleFighter,
    };

    /// <summary>
    /// The old and new values of everything an edit changed, one per line.
    /// </summary>
    private static string Changes(string fighterName, IReadOnlyList<FighterFieldChange> changes)
    {
        string heading = $"Updated {fighterName}:";
        List<string> lines = [.. changes.Select(Line)];

        string message = Join(heading, lines, lines.Count, 0);

        if (message.Length <= Replies.MaxMessageLength)
        {
            return message;
        }

        int kept = lines.Count;

        do
        {
            kept--;
            message = Join(heading, lines, kept, lines.Count - kept);
        }
        while (kept > 1 && message.Length > Replies.MaxMessageLength);

        return Truncate(message, Replies.MaxMessageLength);
    }

    private static string Join(string heading, List<string> lines, int kept, int omitted)
    {
        string message = string.Join('\n', lines.Take(kept).Prepend(heading));

        return omitted == 0 ? message : $"{message}\nand {N(omitted)} more.";
    }

    private static string Line(FighterFieldChange change) =>
        $"• {Label(change.Field)}: {Value(change.Field, change.Before)} → "
        + Value(change.Field, change.After);

    /// <summary>
    /// One stored value as the editor reads it. The repository hands over the value as data, so
    /// the flag arrives as <c>true</c> or <c>false</c> and reads as Yes or No here.
    /// </summary>
    private static string Value(FighterField field, string? value) => value switch
    {
        null or "" => NotSet,
        _ when field is FighterField.RaidBoss =>
            string.Equals(value, "true", StringComparison.Ordinal) ? "Yes" : "No",
        _ => Truncate(value, MaxValueLength),
    };

    private static string Blank(FighterField? field) =>
        $"The {Label(field)} cannot be blank.";

    private static string TooLong() =>
        $"An ability is at most {N(Data.Entities.SignatureAbility.MaxTextLength)} characters, "
        + "the most a Discord embed field shows. Shorten it and try again.";

    private static string NotAllowed(FighterField? field, string? given) => field switch
    {
        FighterField.Character =>
            $"Stanley has no character called ‘{given}’. Pick one of the suggestions.",
        FighterField.Rarity =>
            $"‘{given}’ is not one of Stanley's rarities. Pick one of the four offered.",
        FighterField.Element =>
            $"‘{given}’ is not one of Stanley's elements. Pick one of the six offered.",
        _ => $"‘{given}’ is not a value Stanley holds for the {Label(field)}.",
    };

    private static string AbilityRange(string fighterName, int abilityCount) =>
        $"{fighterName} has {Abilities(abilityCount)}, so the SA number has to be between 1 "
        + $"and {N(abilityCount + 1)}: an ability is added at the end of the block, and the "
        + "numbers never have a gap.";

    private static string Label(FighterField? field) => field switch
    {
        FighterField.Name => "name",
        FighterField.Character => "character",
        FighterField.Rarity => "rarity",
        FighterField.Element => "element",
        FighterField.AbilityName => "ability name",
        FighterField.BaseAtk => "base attack",
        FighterField.BaseHp => "base health",
        FighterField.BaseFs => "base fighter score",
        FighterField.PortraitUrl => "portrait URL",
        FighterField.WikiUrl => "wiki URL",
        FighterField.Quote => "quote",
        FighterField.RaidBoss => "Raid Boss flag",
        FighterField.AbilityText => "ability text",
        _ => "value",
    };

    private static string Abilities(int count) =>
        Counted(count, "signature ability", "signature abilities");

    private static string Aliases(int count) => Counted(count, "alias", "aliases");

    private static string Counted(int count, string singular, string plural) =>
        $"{N(count)} {(count == 1 ? singular : plural)}";

    private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Truncate(string text, int limit) =>
        text.Length <= limit ? text : text[..limit];
}
