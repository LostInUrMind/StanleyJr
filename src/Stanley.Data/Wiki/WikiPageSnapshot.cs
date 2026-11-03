using Stanley.Data.Entities;

namespace Stanley.Data.Wiki;

/// <summary>
/// A copy of one <c>wiki_pages</c> row, as it is stored in a change record's <c>before</c> and
/// <c>after</c>.
/// </summary>
/// <remarks>
/// A snapshot type rather than the entity, as every other snapshot in the solution is: every
/// column is here, so an undo can restore the row exactly as it was.
/// </remarks>
internal sealed record WikiPageSnapshot(
    int Id,
    string Key,
    string KeySlug,
    string Url,
    long? CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static WikiPageSnapshot Of(WikiPage page) => new(
        page.Id,
        page.Key,
        page.KeySlug,
        page.Url,
        page.CreatedByUserId,
        page.CreatedAt,
        page.UpdatedAt);
}
