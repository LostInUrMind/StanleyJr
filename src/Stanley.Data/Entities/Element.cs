namespace Stanley.Data.Entities;

/// <summary>
/// Fire, Water, Air, Light, Dark or Neutral: six seeded rows (database design section 3.5).
/// </summary>
/// <remarks>
/// A table rather than a C# enum because it carries the card's side-strip colour
/// (FRD Table A-1). Colour is data, so an editor can correct it without a deploy; the emoji
/// next to it is presentation and lives in the bot's configuration instead (FR-23).
/// </remarks>
public class Element
{
    public short Id { get; set; }

    public required string Name { get; set; }

    /// <summary>The element colour as <c>#RRGGBB</c>, checked by the database.</summary>
    public required string ColorHex { get; set; }

    public ICollection<Variant> Variants { get; } = [];
}
