using Stanley.Data.Entities;

namespace Stanley.Data.Aliases;

/// <summary>
/// A copy of one <c>variant_aliases</c> row, as it is stored in a change record's
/// <c>before</c> and <c>after</c> (FR-15).
/// </summary>
/// <remarks>
/// A snapshot type rather than the entity: serializing <see cref="VariantAlias"/> would follow
/// its <see cref="VariantAlias.Variant"/> navigation into the whole fighter, and the record is
/// meant to hold the row that changed and nothing else. Every column is here, so an undo
/// can restore the row exactly as it was.
/// </remarks>
internal sealed record AliasSnapshot(
    int Id,
    int VariantId,
    string Alias,
    string AliasSlug,
    long? CreatedByUserId,
    DateTime CreatedAt)
{
    public static AliasSnapshot Of(VariantAlias alias) => new(
        alias.Id,
        alias.VariantId,
        alias.Alias,
        alias.AliasSlug,
        alias.CreatedByUserId,
        alias.CreatedAt);
}
