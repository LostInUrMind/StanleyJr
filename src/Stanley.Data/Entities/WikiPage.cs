namespace Stanley.Data.Entities;

/// <summary>
/// A wiki page that is not a fighter, maintained by editors through <c>/wiki-admin</c>.
/// </summary>
/// <remarks>
/// <para>
/// One row is one name for one page. A page players call by two names is two rows sharing a
/// <see cref="Url"/>: <c>Critless Skill Tree</c> and <c>critless</c> both answer with the
/// Critless page, and either can be corrected or removed on its own.
/// </para>
/// <para>
/// The whole URL is stored, unlike a fighter's, which <see cref="Core.Wiki.WikiUrls"/> builds
/// from its name. These pages have no name to build from and no row anywhere else, so the
/// address is the data.
/// </para>
/// </remarks>
public class WikiPage : ITimestamped
{
    public int Id { get; set; }

    /// <summary>
    /// What an editor typed, which is also what <c>/wiki</c>'s suggestions display.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// <see cref="Key"/> normalized by the same <see cref="Core.Naming.NameNormalizer"/> as
    /// <see cref="Variant.Slug"/> and <see cref="VariantAlias.AliasSlug"/>, so a page is found
    /// however it is capitalized or punctuated. Unique, which is what makes a duplicate key
    /// impossible. A clash with a fighter's name or alias cannot be a database constraint —
    /// those live in another table — so <c>/wiki-admin</c> checks for it.
    /// </summary>
    public required string KeySlug { get; set; }

    /// <summary>The page's address, which <c>/wiki</c> answers with unchanged.</summary>
    public required string Url { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
