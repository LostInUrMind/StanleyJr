namespace Stanley.Data.Aliases;

/// <summary>
/// The alias <c>/alias-admin remove</c> deleted, and the fighter it belonged to, so the confirmation
/// can name both (FR-11).
/// </summary>
/// <remarks>
/// <see cref="AliasRepository.RemoveAsync"/> returns null when nothing matched, the same shape
/// every other lookup in the solution uses for "no match". Removal has no third outcome, so it
/// needs no result enum.
/// </remarks>
public sealed record RemovedAlias(string Alias, string FighterName);
