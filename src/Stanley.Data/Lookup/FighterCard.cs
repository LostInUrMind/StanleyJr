using Stanley.Core.Stats;

namespace Stanley.Data.Lookup;

/// <summary>
/// Everything the fighter card shows (FRD Appendix A), read in one query and flattened so the
/// presentation layer needs no entities and no database.
/// </summary>
/// <remarks>
/// <see cref="Max"/> is computed on the way out of <see cref="FighterLookup"/> and never
/// stored (database design section 6.2).
/// </remarks>
public sealed record FighterCard(
    string Name,
    string Slug,
    string TierName,
    string ElementName,
    string ElementColorHex,
    string CharacterName,
    string SaName,
    int BaseAtk,
    int BaseHp,
    int BaseFs,
    MaxStats Max,
    string? ImageUrl,
    bool IsRaidBoss,
    IReadOnlyList<FighterAbility> Abilities);

/// <summary>
/// One signature ability, already in SA order. Playable fighters have two; Raid Bosses can
/// have more, and every one of them is shown (FR-04).
/// </summary>
public sealed record FighterAbility(short Ordinal, string Text);
