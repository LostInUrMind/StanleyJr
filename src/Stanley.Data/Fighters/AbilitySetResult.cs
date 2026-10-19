namespace Stanley.Data.Fighters;

/// <summary>
/// The outcome of writing one signature ability, which is what the form's private confirmation
/// reports (FR-13).
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="FighterName">
/// The fighter whose ability it is. Null when the fighter has been removed since the form was
/// opened.
/// </param>
/// <param name="Ordinal">The SA number that was written.</param>
/// <param name="WasAdded">
/// True when the ability is new, false when it replaced the text that was there. The editor
/// opened one form either way, so the confirmation is what tells them which it was.
/// </param>
/// <param name="AbilityCount">
/// How many abilities the fighter has: after the write when it succeeded, and as it stands when
/// the SA number was refused.
/// </param>
public sealed record AbilitySetResult(
    FighterWriteOutcome Outcome,
    string? FighterName,
    short Ordinal,
    bool WasAdded,
    int AbilityCount)
{
    public static AbilitySetResult Saved(
        string fighterName,
        short ordinal,
        bool wasAdded,
        int abilityCount) =>
        new(FighterWriteOutcome.Ok, fighterName, ordinal, wasAdded, abilityCount);

    public static AbilitySetResult NotFound() =>
        new(FighterWriteOutcome.FighterNotFound, null, 0, false, 0);

    public static AbilitySetResult Missing() =>
        new(FighterWriteOutcome.ValueMissing, null, 0, false, 0);

    public static AbilitySetResult TooLong() =>
        new(FighterWriteOutcome.AbilityTextTooLong, null, 0, false, 0);

    /// <summary>
    /// The block changed between the form opening and coming back, and this SA number would
    /// now leave a gap (database design section 3.3).
    /// </summary>
    public static AbilitySetResult OutOfRange(string fighterName, int abilityCount) =>
        new(FighterWriteOutcome.AbilityNumberOutOfRange, fighterName, 0, false, abilityCount);
}
