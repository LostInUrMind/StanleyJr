namespace Stanley.Data.Fighters;

/// <summary>
/// The outcome of removing a fighter (FR-14).
/// </summary>
/// <remarks>
/// The counts are reported because the editor confirmed removing more than one row: a
/// fighter's abilities and its aliases go with it, each recorded so a developer can put them
/// back (FR-15).
/// </remarks>
/// <param name="Outcome">What happened.</param>
/// <param name="FighterName">The fighter that was removed.</param>
/// <param name="RemovedAbilities">How many signature abilities went with it.</param>
/// <param name="RemovedAliases">How many aliases went with it.</param>
public sealed record FighterRemovalResult(
    FighterWriteOutcome Outcome,
    string? FighterName,
    int RemovedAbilities,
    int RemovedAliases)
{
    public static FighterRemovalResult Removed(
        string fighterName,
        int removedAbilities,
        int removedAliases) =>
        new(FighterWriteOutcome.Ok, fighterName, removedAbilities, removedAliases);

    public static FighterRemovalResult NotFound() =>
        new(FighterWriteOutcome.FighterNotFound, null, 0, 0);
}
