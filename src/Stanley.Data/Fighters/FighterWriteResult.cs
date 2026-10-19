namespace Stanley.Data.Fighters;

/// <summary>
/// What an editor write did, or the rule that refused it (FR-12 to FR-14). Shared by every
/// result type in this folder, so one refusal is decided in one place however it was reached.
/// </summary>
public enum FighterWriteOutcome
{
    /// <summary>
    /// Nothing was refused: the write went through, or, for <see cref="AbilityPrompt"/>, the
    /// form may be opened.
    /// </summary>
    Ok,

    /// <summary>The <c>fighter</c> option matched no fighter (FR-05).</summary>
    FighterNotFound,

    /// <summary>
    /// The name normalizes to nothing — punctuation or symbols alone — so no lookup could ever
    /// find the fighter again (FR-02).
    /// </summary>
    NameNotUsable,

    /// <summary>
    /// The name is already a fighter's name or an alias, so the write is refused naming what
    /// uses it (database design section 3.2, the rule FR-10 states for aliases).
    /// </summary>
    NameTaken,

    /// <summary>A required value was blank or whitespace.</summary>
    ValueMissing,

    /// <summary>
    /// The value is not one of the rows it has to name: a character, a rarity or an element.
    /// </summary>
    ValueNotAllowed,

    /// <summary>A stat was zero or negative. Stats are whole numbers above zero (FR-13).</summary>
    ValueNotPositive,

    /// <summary>A URL option was not an absolute <c>http</c> or <c>https</c> address.</summary>
    ValueNotAUrl,

    /// <summary>
    /// Every value given already matches what is stored, so there was nothing to do.
    /// </summary>
    NothingToChange,

    /// <summary>
    /// The SA number is neither one of the fighter's abilities nor the next one after them. An
    /// ability is added at the end of the block (database design section 3.3).
    /// </summary>
    AbilityNumberOutOfRange,

    /// <summary>The ability text is longer than a Discord embed field shows (FR-13).</summary>
    AbilityTextTooLong,

    /// <summary>There is no ability block to remove.</summary>
    NoAbilities,
}

/// <summary>
/// The outcome of <c>/fighter-admin add</c> or <c>/fighter-admin edit</c>, with whatever the
/// private confirmation has to show (FR-12, FR-13).
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="FighterName">
/// The fighter the write concerned: the one that was saved, or the name that was refused. Null
/// when no fighter is involved in the outcome.
/// </param>
/// <param name="Field">
/// The option a refusal is about, for the outcomes that turn on one value. The reply layer
/// turns it into that option's name; nothing here is wording.
/// </param>
/// <param name="Detail">
/// The value the outcome turns on, as data: the fighter that already uses the name, or the
/// character, rarity or element that matched no row. Null when the outcome needs no value.
/// </param>
/// <param name="Changes">
/// The fields an edit actually changed, old and new, which FR-13 requires the confirmation to
/// show. Empty for every other outcome.
/// </param>
public sealed record FighterWriteResult(
    FighterWriteOutcome Outcome,
    string? FighterName,
    FighterField? Field,
    string? Detail,
    IReadOnlyList<FighterFieldChange> Changes)
{
    /// <summary>
    /// A fighter that was added: nothing existed before, so there is nothing to show.
    /// </summary>
    public static FighterWriteResult Added(string fighterName) =>
        new(FighterWriteOutcome.Ok, fighterName, null, null, []);

    /// <summary>A fighter that was changed, with the old and new values of what changed.</summary>
    public static FighterWriteResult Edited(
        string fighterName,
        IReadOnlyList<FighterFieldChange> changes) =>
        new(FighterWriteOutcome.Ok, fighterName, null, null, changes);

    public static FighterWriteResult NotFound() =>
        new(FighterWriteOutcome.FighterNotFound, null, null, null, []);

    public static FighterWriteResult NameNotUsable(string name) =>
        new(FighterWriteOutcome.NameNotUsable, name, FighterField.Name, null, []);

    public static FighterWriteResult NameTaken(string name, string owner) =>
        new(FighterWriteOutcome.NameTaken, name, FighterField.Name, owner, []);

    public static FighterWriteResult Missing(FighterField field) =>
        new(FighterWriteOutcome.ValueMissing, null, field, null, []);

    public static FighterWriteResult NotAllowed(FighterField field, string? given) =>
        new(FighterWriteOutcome.ValueNotAllowed, null, field, given, []);

    public static FighterWriteResult NotPositive(FighterField field) =>
        new(FighterWriteOutcome.ValueNotPositive, null, field, null, []);

    public static FighterWriteResult NotAUrl(FighterField field, string? given) =>
        new(FighterWriteOutcome.ValueNotAUrl, null, field, given, []);

    public static FighterWriteResult NothingToChange(string fighterName) =>
        new(FighterWriteOutcome.NothingToChange, fighterName, null, null, []);
}
