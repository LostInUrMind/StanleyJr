namespace Stanley.Bot.Configuration;

/// <summary>
/// The emoji shown on a fighter card, bound from the <c>Presentation</c> configuration
/// section.
/// </summary>
/// <remarks>
/// <para>
/// Emoji are presentation and live here; colour is data and lives in
/// <c>elements.color_hex</c> so an editor can correct it without a deploy.
/// </para>
/// <para>
/// Each value is a full Discord emoji mention, for example
/// <c>&lt;:Diamond:1234567890&gt;</c>. They are uploaded as the application's own emojis so
/// they render in servers Stanley is not a member of. If no emojis are uploaded the
/// dictionaries stay empty and the card simply shows text so nothing here blocks the command.
/// </para>
/// <para>
/// Keys are matched case-insensitively against the names in the database: tier and element
/// names as seeded, character names as imported.
/// </para>
/// </remarks>
public sealed class PresentationOptions
{
    public const string SectionName = "Presentation";

    /// <summary>Keyed by tier name: Bronze, Silver, Gold, Diamond.</summary>
    public Dictionary<string, string> TierEmoji { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Keyed by element name: Fire, Water, Air, Light, Dark, Neutral.</summary>
    public Dictionary<string, string> ElementEmoji { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Keyed by character name, for example Big Band.</summary>
    public Dictionary<string, string> CharacterEmoji { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Keyed by stat abbreviation: FS, ATK, HP.</summary>
    public Dictionary<string, string> StatEmoji { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
