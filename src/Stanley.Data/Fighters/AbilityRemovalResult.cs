namespace Stanley.Data.Fighters;

/// <summary>
/// The outcome of removing a fighter's whole ability block (FR-13).
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="FighterName">The fighter whose block it was.</param>
/// <param name="RemovedCount">How many abilities were removed.</param>
public sealed record AbilityRemovalResult(
    FighterWriteOutcome Outcome,
    string? FighterName,
    int RemovedCount)
{
    public static AbilityRemovalResult Removed(string fighterName, int removedCount) =>
        new(FighterWriteOutcome.Ok, fighterName, removedCount);

    public static AbilityRemovalResult NotFound() =>
        new(FighterWriteOutcome.FighterNotFound, null, 0);

    public static AbilityRemovalResult None(string fighterName) =>
        new(FighterWriteOutcome.NoAbilities, fighterName, 0);
}
