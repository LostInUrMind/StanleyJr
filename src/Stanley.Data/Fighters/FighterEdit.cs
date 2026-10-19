namespace Stanley.Data.Fighters;

/// <summary>
/// The fields an editor is changing on a fighter that already exists: only the ones given are
/// touched (FR-13).
/// </summary>
/// <remarks>
/// <para>
/// Every property is null when the editor left that option out, which is how "giving only the
/// fields to change" reaches the repository. A value equal to what is already stored is
/// dropped as well, so the confirmation lists what actually changed and the change record
/// holds a real change.
/// </para>
/// <para>
/// There is no way to clear a nullable column back to empty: a Discord option is either given
/// or absent, and a magic value meaning "empty" would stop that value being usable as itself.
/// A wrong quote, portrait or wiki URL is corrected by overwriting it.
/// </para>
/// </remarks>
public sealed record FighterEdit(
    string? Name = null,
    string? Character = null,
    string? Rarity = null,
    string? Element = null,
    string? AbilityName = null,
    int? BaseAtk = null,
    int? BaseHp = null,
    int? BaseFs = null,
    string? PortraitUrl = null,
    string? WikiUrl = null,
    string? Quote = null,
    bool? IsRaidBoss = null);
