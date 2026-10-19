namespace Stanley.Data.Lookup;

/// <summary>
/// One autocomplete suggestion (FR-03).
/// </summary>
/// <param name="Label">The name or alias that matched, as it is displayed.</param>
/// <param name="FighterName">
/// The fighter the match belongs to. Equal to <paramref name="Label"/> for a name match, and
/// the fighter an alias points at for an alias match, which is what FR-03 requires a typed
/// alias to suggest.
/// </param>
/// <param name="FighterSlug">
/// What is sent back when the suggestion is picked. The fighter's own slug, never the alias's,
/// so <see cref="FighterLookup.FindCardAsync"/> resolves it by exact match.
/// </param>
/// <param name="IsAlias">Whether <paramref name="Label"/> is an alias rather than a name.</param>
public sealed record FighterSuggestion(
    string Label,
    string FighterName,
    string FighterSlug,
    bool IsAlias);
