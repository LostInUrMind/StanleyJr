namespace Stanley.Data.Aliases;

/// <summary>
/// What <c>/alias list</c> shows: a fighter and the other names it answers to (FR-09).
/// </summary>
/// <param name="FighterName">
/// The fighter's display name, not what the user typed: FR-09's reply names the fighter, and
/// the input may have been an alias or a near miss (FR-02).
/// </param>
/// <param name="Aliases">
/// The aliases as the editors typed them, alphabetically. Empty for a fighter that has none,
/// which FR-09 words differently rather than treating as a failure.
/// </param>
public sealed record AliasListing(string FighterName, IReadOnlyList<string> Aliases);
