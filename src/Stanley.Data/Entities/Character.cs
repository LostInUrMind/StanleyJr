namespace Stanley.Data.Entities;

/// <summary>
/// One of the 18 Skullgirls characters that variants belong to
/// (database design section 3.1).
/// </summary>
/// <remarks>
/// The import creates these rows with names only. Every other column is nullable and stays
/// nullable, because the character-wide text is filled in after launch (database design
/// section 5.4), and a command that shows it renders what is there.
/// </remarks>
public class Character : ITimestamped
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Droppable (database design Appendix C).</summary>
    public string? Archetype { get; set; }

    /// <summary>How the character plays: their play style.</summary>
    public string? Description { get; set; }

    /// <summary>The character's story.</summary>
    public string? Bio { get; set; }

    /// <summary>General attack ratio, 1 to 5.</summary>
    public short? AtkRatio { get; set; }

    /// <summary>General health ratio, 1 to 5.</summary>
    public short? HpRatio { get; set; }

    public string? Ability { get; set; }

    public string? MaName { get; set; }

    public string? Ma1Name { get; set; }

    public string? Ma1Des { get; set; }

    public string? Ma2Name { get; set; }

    public string? Ma2Des { get; set; }

    public string? PrestigeName { get; set; }

    public string? PrestigeDes { get; set; }

    /// <summary>URL of the character portrait. Stanley stores the URL, never the image.</summary>
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Variant> Variants { get; } = [];
}
