using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Bot.Presentation;
using Stanley.Core.Stats;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Tests;

/// <summary>
/// What <c>/credits</c> says (FR-18), read from the two places that decide it: the configured
/// icon and the one line of wording.
/// </summary>
public class CreditsTests
{
    private const string Skull = "<:SkullEmote:1560976711707918486>";

    [Fact]
    public void The_credits_name_the_team_after_the_skull()
    {
        FighterPresentation presentation = WithSkull();

        Assert.Equal(
            $"Developed by {Skull} The SGM Wiki Team",
            Replies.Credits(presentation.FighterScoreIcon));
    }

    [Fact]
    public void The_credits_read_without_the_skull_until_the_emoji_is_uploaded()
    {
        // Every emoji is optional, and here there is no label for a missing one to fall back
        // to (FR-23), so the sentence simply has one fewer word in it.
        var presentation = new FighterPresentation(Options.Create(new PresentationOptions()));

        Assert.Null(presentation.FighterScoreIcon);
        Assert.Equal("Developed by The SGM Wiki Team", Replies.Credits(presentation.FighterScoreIcon));

        // A key set to blank is the same as no key: both mean nothing was configured.
        Assert.Equal("Developed by The SGM Wiki Team", Replies.Credits("   "));
    }

    [Fact]
    public void The_credits_skull_is_the_one_the_card_shows()
    {
        // The point of reading it from the card's own configuration: one icon, configured once.
        FighterPresentation presentation = WithSkull();

        Assert.Contains(Skull, presentation.StatsBlock(Card()), StringComparison.Ordinal);
        Assert.Contains(
            Skull,
            Replies.Credits(presentation.FighterScoreIcon),
            StringComparison.Ordinal);
    }

    private static FighterPresentation WithSkull() =>
        new(Options.Create(new PresentationOptions { StatEmoji = { ["FS"] = Skull } }));

    /// <summary>Any fighter: this is about the stat icons, not the fighter.</summary>
    private static FighterCard Card() => new(
        "Brass Bandit",
        "brassbandit",
        "Diamond",
        "Dark",
        "#9B59B6",
        "Big Band",
        "RIFF WRATH",
        617,
        4169,
        919,
        new MaxStats(11847, 80045, 9400),
        null,
        false,
        []);
}
