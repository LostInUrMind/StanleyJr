namespace Stanley.Data.Aliases;

/// <summary>
/// What <c>/alias list</c> shows: a fighter and the other names it answers to.
/// </summary>
/// <param name="FighterName">
/// The fighter's display name, not what the user typed: the reply names the fighter, and
/// the input may have been an alias or a near miss.
/// </param>
/// <param name="Aliases">
/// The aliases as the editors typed them, alphabetically. Empty for a fighter that has none,
/// which is worded differently rather than treated as a failure.
/// </param>
public sealed record AliasListing(string FighterName, IReadOnlyList<string> Aliases);
