namespace Stanley.Data.Views;

/// <summary>
/// Read-only projection of the <c>searchable_names</c> view: every variant name and every
/// alias in one place (database design section 6.3).
/// </summary>
public class SearchableName
{
    public int VariantId { get; set; }

    /// <summary>The name or alias as it is displayed.</summary>
    public string Label { get; set; } = null!;

    /// <summary>The normalized form, for exact matching.</summary>
    public string Slug { get; set; } = null!;

    /// <summary><c>name</c> or <c>alias</c>.</summary>
    public string Kind { get; set; } = null!;
}
