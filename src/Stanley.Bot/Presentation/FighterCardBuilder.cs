using Discord;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Presentation;

/// <summary>
/// One message of a fighter card: the embed, and the message text that goes with it.
/// </summary>
public sealed record FighterCardMessage(string? Text, Embed Embed);

/// <summary>
/// Builds the fighter card (FRD Appendix A) and splits it when it outgrows what Discord will
/// accept in one message (FR-26).
/// </summary>
/// <remarks>
/// Nothing here touches Discord or the database.
/// </remarks>
public sealed class FighterCardBuilder(FighterPresentation presentation)
{
    /// <summary>Discord accepts at most this many fields in one embed.</summary>
    private const int MaxFieldsPerEmbed = 25;

    private const int MaxCharactersPerMessage = 6000;

    private const int MaxTitleLength = 256;

    private const int MaxFieldNameLength = 256;

    /// <summary>
    /// The most an embed field displays.
    /// </summary>
    private const int MaxFieldValueLength = 1024;

    private const string StatsFieldName = "STATS";

    /// <summary>
    /// Builds the messages for one fighter, in order.
    /// </summary>
    public IReadOnlyList<FighterCardMessage> Build(FighterCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        string title = Truncate(presentation.CardTitle(card), MaxTitleLength);
        Color color = FighterPresentation.ElementColor(card.ElementColorHex);
        string statsValue = presentation.StatsBlock(card);

        var abilities = new Queue<AbilityField>(card.Abilities.Select(ability => new AbilityField(
            Truncate(FighterPresentation.AbilityTitle(card, ability), MaxFieldNameLength),
            Truncate(ability.Text, MaxFieldValueLength))));

        var messages = new List<FighterCardMessage>();

        EmbedBuilder first = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(color);

        if (IsDisplayableUrl(card.ImageUrl))
        {
            first.WithThumbnailUrl(card.ImageUrl);
        }

        int reserved = title.Length + StatsFieldName.Length + statsValue.Length;
        Fill(first, abilities, reserved, MaxFieldsPerEmbed - 1, forceFirst: false);
        first.AddField(StatsFieldName, statsValue);

        messages.Add(new FighterCardMessage(
            Format.Underline(Format.Bold(Format.Sanitize(card.Name))),
            first.Build()));

        while (abilities.Count > 0)
        {
            string continuedTitle = Truncate(
                $"{Format.Sanitize(card.Name)} (continued)",
                MaxTitleLength);

            EmbedBuilder next = new EmbedBuilder()
                .WithTitle(continuedTitle)
                .WithColor(color);

            Fill(next, abilities, continuedTitle.Length, MaxFieldsPerEmbed, forceFirst: true);
            messages.Add(new FighterCardMessage(null, next.Build()));
        }

        return messages;
    }

    /// <summary>
    /// Moves abilities out of <paramref name="pending"/> and into <paramref name="embed"/>
    /// while they fit, in SA order.
    /// </summary>
    /// <param name="used">Characters the embed has already committed to.</param>
    /// <param name="slots">Fields the embed may still add.</param>
    /// <param name="forceFirst">
    /// Whether an empty embed must take one ability even if the budget says no.
    /// </param>
    private static void Fill(
        EmbedBuilder embed,
        Queue<AbilityField> pending,
        int used,
        int slots,
        bool forceFirst)
    {
        while (pending.Count > 0 && slots > 0)
        {
            AbilityField next = pending.Peek();
            bool fits = used + next.Cost <= MaxCharactersPerMessage;

            if (!fits && !(forceFirst && embed.Fields.Count == 0))
            {
                return;
            }

            embed.AddField(next.Name, next.Value);
            pending.Dequeue();
            used += next.Cost;
            slots--;
        }
    }

    /// <summary>
    /// Whether a stored portrait URL can be handed to Discord. A
    /// </summary>
    private static bool IsDisplayableUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
        && (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>One ability as a field, with what it costs against the message budget.</summary>
    private sealed record AbilityField(string Name, string Value)
    {
        public int Cost => Name.Length + Value.Length;
    }
}
