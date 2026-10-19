namespace Stanley.Data.Views;

/// <summary>
/// Read-only projection of the <c>tier_variant_counts</c> view (database design section 6.1).
/// </summary>
public class TierVariantCount
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public long TotalVariants { get; set; }
}
