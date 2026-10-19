namespace Stanley.Data.Aliases;

/// <summary>
/// One autocomplete choice for <c>/alias-admin remove</c>: an alias that exists, and the fighter it
/// belongs to.
/// </summary>
/// <remarks>
/// <c>/alias-admin remove</c> takes the alias, not the fighter (database design section 3.6), so
/// this is not <see cref="Lookup.FighterSuggestion"/>: it suggests aliases only, and a fighter
/// with no aliases never appears.
/// </remarks>
/// <param name="Alias">The alias as the editor typed it, for the choice's label.</param>
/// <param name="AliasSlug">
/// What is sent back when the choice is picked. The slug rather than the alias, so the removal
/// matches on <c>alias_slug</c> exactly however the label is punctuated.
/// </param>
/// <param name="FighterName">The fighter it belongs to, shown beside the alias.</param>
public sealed record AliasSuggestion(string Alias, string AliasSlug, string FighterName);
