using Stanley.Data.Entities;

namespace Stanley.Data.Fighters;

/// <summary>
/// A copy of one <c>signature_abilities</c> row, as it is stored in a change record's
/// <c>before</c> and <c>after</c> (FR-15).
/// </summary>
/// <remarks>
/// Removing a block writes one of these per ability rather than one record for the block. The
/// rows are what an undo would put back, and a record per row is the only shape that can put
/// them back individually (FR-16).
/// </remarks>
internal sealed record AbilitySnapshot(
    int VariantId,
    short Ordinal,
    string AbilityText,
    DateTime UpdatedAt)
{
    public static AbilitySnapshot Of(SignatureAbility ability) => new(
        ability.VariantId,
        ability.Ordinal,
        ability.AbilityText,
        ability.UpdatedAt);
}
