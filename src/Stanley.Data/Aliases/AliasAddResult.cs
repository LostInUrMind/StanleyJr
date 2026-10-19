namespace Stanley.Data.Aliases;

/// <summary>
/// Why <c>/alias-admin add</c> did or did not add the alias (FR-10).
/// </summary>
public enum AliasAddOutcome
{
    Added,
    FighterNotFound,
    AliasNotUsable,
    AliasTaken,
    FighterNameTaken,
}

/// <summary>
/// The outcome of <c>/alias-admin add</c>, with whatever the reply has to name (FR-10).
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Alias">The alias as the editor typed it, trimmed.</param>
/// <param name="FighterName">
/// The fighter the alias belongs to: the one it was added to, or, when it was refused, the one
/// that already uses it. Null when no
/// fighter is involved in the outcome.
/// </param>
public sealed record AliasAddResult(
    AliasAddOutcome Outcome,
    string Alias,
    string? FighterName)
{
    public static AliasAddResult Added(string alias, string fighterName) =>
        new(AliasAddOutcome.Added, alias, fighterName);

    public static AliasAddResult FighterNotFound(string alias) =>
        new(AliasAddOutcome.FighterNotFound, alias, null);

    public static AliasAddResult AliasNotUsable(string alias) =>
        new(AliasAddOutcome.AliasNotUsable, alias, null);

    public static AliasAddResult AliasTaken(string alias, string fighterName) =>
        new(AliasAddOutcome.AliasTaken, alias, fighterName);

    public static AliasAddResult FighterNameTaken(string alias, string fighterName) =>
        new(AliasAddOutcome.FighterNameTaken, alias, fighterName);
}
