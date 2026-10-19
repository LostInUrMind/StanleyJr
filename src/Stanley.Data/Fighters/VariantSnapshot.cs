using Stanley.Data.Entities;

namespace Stanley.Data.Fighters;

/// <summary>
/// A copy of one <c>variants</c> row, as it is stored in a change record's <c>before</c> and
/// <c>after</c> (FR-15).
/// </summary>
/// <remarks>
/// A snapshot type rather than the entity, for the reason <c>AliasSnapshot</c> gives:
/// serializing <see cref="Variant"/> would follow its navigations into the character, the tier
/// and every ability, and a record holds the row that changed and nothing else. Every column
/// is here, foreign keys included, so an undo (FR-16) can restore the row exactly as it was.
/// </remarks>
internal sealed record VariantSnapshot(
    int Id,
    int CharacterId,
    string Name,
    string Slug,
    string? Quote,
    short TierId,
    short ElementId,
    string SaName,
    int BaseAtk,
    int BaseHp,
    int BaseFs,
    bool IsRaidBoss,
    string? WikiUrl,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static VariantSnapshot Of(Variant variant) => new(
        variant.Id,
        variant.CharacterId,
        variant.Name,
        variant.Slug,
        variant.Quote,
        variant.TierId,
        variant.ElementId,
        variant.SaName,
        variant.BaseAtk,
        variant.BaseHp,
        variant.BaseFs,
        variant.IsRaidBoss,
        variant.WikiUrl,
        variant.ImageUrl,
        variant.CreatedAt,
        variant.UpdatedAt);
}
