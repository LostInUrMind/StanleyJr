namespace Stanley.Data.Entities;

/// <summary>
/// An alternative name players use for a variant (database design section 3.6).
/// </summary>
public class VariantAlias
{
    public int Id { get; set; }

    public int VariantId { get; set; }

    public Variant Variant { get; set; } = null!;

    /// <summary>The alias as the editor typed it, for display by <c>/alias list</c>.</summary>
    public required string Alias { get; set; }

    /// <summary>
    /// <see cref="Alias"/> normalized by the same <see cref="Core.Naming.NameNormalizer"/> as
    /// <see cref="Variant.Slug"/>. Unique, which is what makes a duplicate alias impossible
    /// (FR-10). A collision with a variant's own slug cannot be a database constraint, so the
    /// command checks for it.
    /// </summary>
    public required string AliasSlug { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
