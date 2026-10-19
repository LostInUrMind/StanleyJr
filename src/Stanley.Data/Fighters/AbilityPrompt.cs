namespace Stanley.Data.Fighters;

/// <summary>
/// What the pop-up form for one signature ability needs to be opened: which ability it is, and
/// the text to pre-fill when that ability already exists (FR-13).
/// </summary>
/// <remarks>
/// One ability per form, because a Discord form holds at most five components and a Raid Boss
/// can have more abilities than that (database design section 3.3). The ability is addressed
/// by <see cref="VariantId"/> and <see cref="Ordinal"/>, which is the row's whole key, so the
/// form can carry it and nothing has to be resolved again when it comes back.
/// </remarks>
/// <param name="Outcome">
/// <see cref="FighterWriteOutcome.Ok"/> when the form may be opened.
/// </param>
/// <param name="VariantId">The fighter the ability belongs to.</param>
/// <param name="FighterName">Its name, for the form's title. Null when no fighter matched.</param>
/// <param name="Ordinal">
/// The SA number the form will write: the one the editor asked for, or one past the fighter's
/// highest when they asked for none.
/// </param>
/// <param name="CurrentText">
/// The ability's current text, which the form is pre-filled with, or null when this SA number
/// is new.
/// </param>
/// <param name="AbilityCount">
/// How many abilities the fighter has, which is what says the SA numbers an editor may give.
/// </param>
public sealed record AbilityPrompt(
    FighterWriteOutcome Outcome,
    int VariantId,
    string? FighterName,
    short Ordinal,
    string? CurrentText,
    int AbilityCount)
{
    public static AbilityPrompt Ready(
        int variantId,
        string fighterName,
        short ordinal,
        string? currentText,
        int abilityCount) =>
        new(FighterWriteOutcome.Ok, variantId, fighterName, ordinal, currentText, abilityCount);

    public static AbilityPrompt NotFound() =>
        new(FighterWriteOutcome.FighterNotFound, 0, null, 0, null, 0);

    /// <summary>
    /// The SA number is neither an ability the fighter has nor the next one after them, so
    /// writing it would leave a gap in the numbers (database design section 3.3).
    /// </summary>
    public static AbilityPrompt OutOfRange(string fighterName, int abilityCount) =>
        new(
            FighterWriteOutcome.AbilityNumberOutOfRange,
            0,
            fighterName,
            0,
            null,
            abilityCount);
}
