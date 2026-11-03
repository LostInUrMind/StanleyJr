using System.Globalization;
using Discord;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Presentation;

/// <summary>
/// Turns a fighter's data into the strings a card shows: the one place that knows which emoji
/// goes with a tier, an element, a character or a stat.
/// </summary>
/// <remarks>
/// Every emoji is optional. When one is not configured the card falls back to text, so the
/// command works before the application emojis have been uploaded and keeps working
/// if one is later removed.
/// </remarks>
public sealed class FighterPresentation(IOptions<PresentationOptions> options)
{
    private readonly PresentationOptions _options = options.Value;

    /// <summary>The label used for a stat when no emoji is configured for it.</summary>
    private const string FighterScore = "FS";

    private const string Attack = "ATK";

    private const string Health = "HP";

    /// <summary>The name of the editor role whose icon <c>/credits</c> shows.</summary>
    private const string WikiEditor = "Wiki Editor";

    /// <summary>
    /// The Wiki Editor icon, shown by <c>/credits</c> before the team's name.
    /// </summary>
    /// <remarks>
    /// Configured like every other emoji, so it is changed without a deploy and the credits
    /// simply read without it until it has been uploaded.
    /// </remarks>
    public string? WikiEditorIcon => Emoji(_options.RoleEmoji, WikiEditor);

    /// <summary>
    /// The card title: rarity, element and character, each with its icon
    /// (FRD Appendix A).
    /// </summary>
    public string CardTitle(FighterCard card) => string.Join(
        ' ',
        Labelled(_options.TierEmoji, card.TierName, card.TierName),
        Labelled(_options.ElementEmoji, card.ElementName, card.ElementName),
        Labelled(_options.CharacterEmoji, card.CharacterName, card.CharacterName));

    /// <summary>
    /// The STATS field.
    /// </summary>
    public string StatsBlock(FighterCard card) =>
        $"Min {Stat(FighterScore, card.BaseFs)}  {Stat(Attack, card.BaseAtk)}  "
        + $"{Stat(Health, card.BaseHp)}\n"
        + $"Max {Stat(FighterScore, card.Max.Fs)}  {Stat(Attack, card.Max.Atk)}  "
        + $"{Stat(Health, card.Max.Hp)}";

    /// <summary>
    /// The quote shown above the abilities: the stored text in quotation marks, in italics.
    /// Null when the fighter has no quote, which is what keeps the line off the card.
    /// </summary>
    /// <remarks>
    /// Discord markdown has no way to centre a line, so the quote sits left-aligned like every
    /// other line of the card. The text is sanitised because an editor's quote is free text and
    /// a stray <c>*</c> would otherwise end the italics early.
    /// </remarks>
    public static string? QuoteLine(string? quote) =>
        string.IsNullOrWhiteSpace(quote)
            ? null
            : Format.Italics($"\"{Format.Sanitize(quote.Trim())}\"");

    /// <summary>
    /// The field name for one signature ability.
    /// </summary>
    public static string AbilityTitle(FighterCard card, FighterAbility ability) =>
        $"{card.SaName} (SA{ability.Ordinal.ToString(CultureInfo.InvariantCulture)})";

    /// <summary>
    /// The card's side strip: the element colour from the database (FRD Table A-1).
    /// </summary>
    public static Color ElementColor(string colorHex) =>
        colorHex.Length == 7
        && colorHex[0] == '#'
        && uint.TryParse(
            colorHex.AsSpan(1),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out uint packed)
            ? new Color(packed)
            : Color.Default;

    private string Stat(string abbreviation, int value) =>
        $"{Labelled(_options.StatEmoji, abbreviation, abbreviation)} "
        + value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The emoji configured for <paramref name="key"/> followed by <paramref name="text"/>, or
    /// <paramref name="text"/> alone when there is no emoji.
    /// </summary>
    private static string Labelled(
        Dictionary<string, string> emoji,
        string key,
        string text) =>
        Emoji(emoji, key) is string mention ? $"{mention} {text}" : text;

    /// <summary>
    /// The emoji configured for <paramref name="key"/>, or null when there is none.
    /// </summary>
    private static string? Emoji(Dictionary<string, string> emoji, string key) =>
        emoji.TryGetValue(key, out string? mention) && !string.IsNullOrWhiteSpace(mention)
            ? mention
            : null;
}
