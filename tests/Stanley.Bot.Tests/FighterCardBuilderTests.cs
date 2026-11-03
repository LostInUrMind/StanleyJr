using Discord;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Bot.Presentation;
using Stanley.Core.Stats;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Tests;

/// <summary>
/// The card layout (FRD Appendix A) and the split that keeps a long card inside Discord's
/// limits.
/// </summary>
/// <remarks>
/// No Discord connection and no database: <see cref="FighterCardBuilder"/> takes a
/// <see cref="FighterCard"/> and returns messages, so a Raid Boss that does not exist can be
/// handed to it.
/// </remarks>
public class FighterCardBuilderTests
{
    /// <summary>Discord's limit on fields in one embed.</summary>
    private const int MaxFieldsPerEmbed = 25;

    /// <summary>Discord's limit on characters across a message's embeds.</summary>
    private const int MaxCharactersPerMessage = 6000;

    private static readonly FighterCardBuilder Builder =
        new(new FighterPresentation(Options.Create(new PresentationOptions())));

    [Fact]
    public void Playable_fighter_fits_in_one_message()
    {
        FighterCard card = Card(abilityCount: 2);

        IReadOnlyList<FighterCardMessage> messages = Builder.Build(card);

        FighterCardMessage only = Assert.Single(messages);

        // The name in bold and underlined, as the message text (FRD Appendix A).
        Assert.Equal("__**Brass Bandit**__", only.Text);

        // Both abilities, then STATS.
        Assert.Equal(3, only.Embed.Fields.Length);
        Assert.Equal("RIFF WRATH (SA1)", only.Embed.Fields[0].Name);
        Assert.Equal("RIFF WRATH (SA2)", only.Embed.Fields[1].Name);
        Assert.Equal("STATS", only.Embed.Fields[2].Name);
    }

    [Fact]
    public void Card_shows_the_element_colour_and_the_portrait()
    {
        FighterCard card = Card(abilityCount: 2);

        Embed embed = Builder.Build(card)[0].Embed;

        // Dark, from FRD Table A-1.
        Assert.Equal(new Color(0x9B59B6), embed.Color);
        Assert.Equal(card.ImageUrl, embed.Thumbnail?.Url);
        Assert.Equal("Diamond Dark Big Band", embed.Title);
    }

    [Fact]
    public void Stats_show_minimum_and_maximum()
    {
        FighterCard card = Card(abilityCount: 2);

        Embed embed = Builder.Build(card)[0].Embed;
        string stats = embed.Fields[^1].Value;

        // The values FRD Appendix A says v2 must produce for Brass Bandit.
        Assert.Equal("Min FS 919  ATK 617  HP 4169\nMax FS 29762  ATK 11847  HP 80045", stats);
    }

    [Fact]
    public void Raid_boss_shows_every_ability()
    {
        // The three current Raid Bosses have five abilities; v1 only ever showed two.
        FighterCard card = Card(abilityCount: 5);

        IReadOnlyList<FighterCardMessage> messages = Builder.Build(card);

        FighterCardMessage only = Assert.Single(messages);
        Assert.Equal(6, only.Embed.Fields.Length);
        Assert.Equal(
            ["SA1", "SA2", "SA3", "SA4", "SA5"],
            only.Embed.Fields[..5].Select(field => field.Name[^4..^1]));
    }

    [Fact]
    public void Oversized_raid_boss_splits_across_messages_in_order()
    {
        // The acceptance criterion: eight abilities of the longest text the database
        // allows, which cannot fit in one message.
        FighterCard card = Card(
            abilityCount: 8,
            abilityText: new string('x', SignatureAbility.MaxTextLength));

        IReadOnlyList<FighterCardMessage> messages = Builder.Build(card);

        Assert.True(messages.Count > 1, "Eight maximum-length abilities must not fit in one message.");

        // Every ability is shown, in SA order, across the messages.
        string[] ordinals =
        [
            .. messages
                .SelectMany(message => message.Embed.Fields)
                .Where(field => field.Name != "STATS")
                .Select(field => field.Name)
        ];

        Assert.Equal(
            [.. Enumerable.Range(1, 8).Select(number => $"RIFF WRATH (SA{number})")],
            ordinals);

        // Each message stays inside both of Discord's limits, or Discord rejects it.
        foreach (FighterCardMessage message in messages)
        {
            Assert.InRange(message.Embed.Fields.Length, 1, MaxFieldsPerEmbed);
            Assert.InRange(message.Embed.Length, 1, MaxCharactersPerMessage);
        }

        // Only the first message repeats the name as text and carries the stats.
        Assert.NotNull(messages[0].Text);
        Assert.Contains(messages[0].Embed.Fields, field => field.Name == "STATS");
        Assert.All(messages.Skip(1), message => Assert.Null(message.Text));
        Assert.DoesNotContain(
            messages.Skip(1).SelectMany(message => message.Embed.Fields),
            field => field.Name == "STATS");
    }

    [Fact]
    public void Quote_sits_above_the_abilities_in_italics_and_quotation_marks()
    {
        FighterCard card = Card(abilityCount: 2, quote: "  Say hello to my little friend.  ");

        Embed embed = Builder.Build(card)[0].Embed;

        // The description renders above the fields, the first of which is SA1.
        // Sanitising escapes the full stop as well; Discord renders it as a full stop.
        Assert.Equal("""*"Say hello to my little friend\."*""", embed.Description);
        Assert.Equal("RIFF WRATH (SA1)", embed.Fields[0].Name);
    }

    [Fact]
    public void Quote_markup_cannot_end_the_italics_early()
    {
        FighterCard card = Card(abilityCount: 2, quote: "*not* my italics");

        Embed embed = Builder.Build(card)[0].Embed;

        Assert.Equal("""*"\*not\* my italics"*""", embed.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Card_without_a_quote_shows_no_quote_line(string? quote)
    {
        FighterCard card = Card(abilityCount: 2, quote: quote);

        Embed embed = Builder.Build(card)[0].Embed;

        Assert.Null(embed.Description);
    }

    [Fact]
    public void Quote_counts_against_the_message_budget()
    {
        // A quote plus eight maximum-length abilities: the quote must not push the first
        // message past what Discord accepts.
        FighterCard card = Card(
            abilityCount: 8,
            abilityText: new string('x', SignatureAbility.MaxTextLength),
            quote: new string('q', 400));

        IReadOnlyList<FighterCardMessage> messages = Builder.Build(card);

        Assert.All(
            messages,
            message => Assert.InRange(message.Embed.Length, 1, MaxCharactersPerMessage));
    }

    [Fact]
    public void Continuation_messages_do_not_repeat_the_quote()
    {
        FighterCard card = Card(
            abilityCount: 8,
            abilityText: new string('x', SignatureAbility.MaxTextLength),
            quote: "Say hello to my little friend.");

        IReadOnlyList<FighterCardMessage> messages = Builder.Build(card);

        Assert.True(messages.Count > 1, "Eight maximum-length abilities must not fit in one message.");
        Assert.All(messages.Skip(1), message => Assert.Null(message.Embed.Description));
    }

    [Fact]
    public void Unusable_portrait_url_does_not_lose_the_card()
    {
        FighterCard card = Card(abilityCount: 2) with { ImageUrl = "not a url" };

        Embed embed = Builder.Build(card)[0].Embed;

        Assert.Null(embed.Thumbnail);
        Assert.Equal(3, embed.Fields.Length);
    }

    /// <summary>
    /// Brass Bandit, the worked example in FRD Appendix A, with however many abilities the
    /// test needs.
    /// </summary>
    private static FighterCard Card(
        int abilityCount,
        string abilityText = "Does a thing.",
        string? quote = null) =>
        new(
            Name: "Brass Bandit",
            Slug: "brassbandit",
            Quote: quote,
            TierName: "Diamond",
            ElementName: "Dark",
            ElementColorHex: "#9B59B6",
            CharacterName: "Big Band",
            SaName: "RIFF WRATH",
            BaseAtk: 617,
            BaseHp: 4169,
            BaseFs: 919,
            Max: StatCalculator.Calculate(617, 4169, evolutionsToMax: 0),
            ImageUrl: "https://example.invalid/brass-bandit.png",
            IsRaidBoss: abilityCount > 2,
            Abilities:
            [
                .. Enumerable
                    .Range(1, abilityCount)
                    .Select(ordinal => new FighterAbility((short)ordinal, abilityText))
            ]);
}
