namespace Stanley.Data.Fighters;

/// <summary>
/// The fighter an editor is adding: exactly the values FR-12 asks for. Its signature abilities
/// are added afterwards, one at a time (FR-13).
/// </summary>
/// <remarks>
/// <see cref="Character"/>, <see cref="Rarity"/> and <see cref="Element"/> are names, matched
/// against the rows those tables hold and refused when they match none. The command offers
/// fixed lists, so a mismatch means the lists and the seeded rows have drifted apart; that is
/// a refusal naming the value, never a guess at which row was meant.
/// </remarks>
public sealed record FighterDraft(
    string? Name,
    string? Character,
    string? Rarity,
    string? Element,
    string? AbilityName,
    int BaseAtk,
    int BaseHp,
    int BaseFs,
    string? PortraitUrl);
