namespace Stanley.Data;

/// <summary>
/// The names of the three views created by the first migration, in one place so the migration,
/// the mappings and the tests cannot disagree (database design sections 6.1 and 6.3).
/// </summary>
public static class DatabaseViews
{
    public const string TierVariantCounts = "tier_variant_counts";

    public const string ElementVariantCounts = "element_variant_counts";

    public const string SearchableNames = "searchable_names";
}
