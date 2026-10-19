namespace Stanley.Data.Fighters;

/// <summary>
/// The fighter a destructive command is about to act on, read before the confirmation step so
/// the editor is told what will go with it (FR-13, FR-14).
/// </summary>
/// <remarks>
/// <see cref="VariantId"/> is what the confirm button carries, so the name is resolved once,
/// while the editor is still looking at the command they typed, rather than again on the
/// button press.
/// </remarks>
/// <param name="VariantId">The row the confirm button acts on.</param>
/// <param name="Name">The fighter's name, as the confirmation shows it.</param>
/// <param name="AbilityCount">How many signature abilities it has.</param>
/// <param name="AliasCount">How many aliases would be removed with it (FR-14).</param>
public sealed record FighterTarget(
    int VariantId,
    string Name,
    int AbilityCount,
    int AliasCount);
