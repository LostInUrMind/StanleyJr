namespace Stanley.Data.Entities;

/// <summary>
/// One collectible variant of a character: 309 at launch, and the table almost every command
/// reads (database design section 3.2).
/// </summary>
public class Variant : ITimestamped
{
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public Character Character { get; set; } = null!;

    /// <summary>Display name, for example <c>Heat Synced</c>. Unique.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// <see cref="Name"/> run through <see cref="Core.Naming.NameNormalizer"/>, which is what
    /// makes the tolerant lookup in FR-02 possible. Unique, and never set by hand.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>The quote from the referenced source.</summary>
    public string? Quote { get; set; }

    public short TierId { get; set; }

    public Tier Tier { get; set; } = null!;

    public short ElementId { get; set; }

    public Element Element { get; set; } = null!;

    public required string SaName { get; set; }

    public int BaseAtk { get; set; }

    public int BaseHp { get; set; }

    public int BaseFs { get; set; }

    /// <summary>
    /// True for a non-playable Raid Boss. Raid Bosses are searchable and have cards like any
    /// other variant, but they are left out of every count because they are not collectible
    /// (database design section 6.1).
    /// </summary>
    public bool IsRaidBoss { get; set; }

    /// <summary>
    /// Override for the wiki link. Normally empty: FR-07 builds the URL from
    /// <see cref="Name"/>. Kept so an editor can fix a page whose title differs.
    /// </summary>
    public string? WikiUrl { get; set; }

    /// <summary>
    /// URL of the variant portrait. Editable so a moved image can be repointed; Stanley does
    /// not host images (FRD section 2.5).
    /// </summary>
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The variant's signature abilities. Ordinals run from 1 with no gap, and the whole block
    /// is added, edited or removed together (database design section 3.3).
    /// </summary>
    public ICollection<SignatureAbility> SignatureAbilities { get; } = [];

    public ICollection<VariantAlias> Aliases { get; } = [];
}
