using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Bot.Presentation;

namespace Stanley.Bot.Tests;

/// <summary>
/// What <c>/credits</c> says, read from the two places that decide it: the configured
/// icon and the one line of wording.
/// </summary>
public class CreditsTests
{
    private const string WikiIcon = "<:wiki_icon:1566466770334777515>";

    private const string Art = "Art by @switch5721 on Discord";

    [Fact]
    public void The_credits_name_the_team_after_the_wiki_icon()
    {
        FighterPresentation presentation = WithWikiIcon();

        Assert.Equal(
            $"Developed by {WikiIcon} The SGM Wiki Team\n{Art}",
            Replies.Credits(presentation.WikiEditorIcon));
    }

    [Fact]
    public void The_credits_name_the_artist_whether_or_not_the_emoji_is_configured()
    {
        Assert.EndsWith(
            Art,
            Replies.Credits(WithWikiIcon().WikiEditorIcon),
            StringComparison.Ordinal);

        Assert.EndsWith(Art, Replies.Credits(null), StringComparison.Ordinal);
    }

    [Fact]
    public void The_credits_read_without_the_icon_until_the_emoji_is_uploaded()
    {
        // Every emoji is optional, and here there is no label for a missing one to fall back
        // to, so the sentence simply has one fewer word in it.
        var presentation = new FighterPresentation(Options.Create(new PresentationOptions()));

        Assert.Null(presentation.WikiEditorIcon);
        Assert.Equal(
            $"Developed by The SGM Wiki Team\n{Art}",
            Replies.Credits(presentation.WikiEditorIcon));

        // A key set to blank is the same as no key: both mean nothing was configured.
        Assert.Equal($"Developed by The SGM Wiki Team\n{Art}", Replies.Credits("   "));
    }

    [Fact]
    public void The_credits_icon_is_the_one_configured_for_the_wiki_editor_role()
    {
        // Keys are matched case-insensitively, as everywhere else in the section, so the
        // casing someone types into appsettings.json cannot lose the icon.
        var presentation = new FighterPresentation(Options.Create(
            new PresentationOptions { RoleEmoji = { ["wiki editor"] = WikiIcon } }));

        Assert.Equal(WikiIcon, presentation.WikiEditorIcon);

        // Another role's icon is not the one /credits shows.
        var other = new FighterPresentation(Options.Create(
            new PresentationOptions { RoleEmoji = { ["Moderator"] = WikiIcon } }));

        Assert.Null(other.WikiEditorIcon);
    }

    private static FighterPresentation WithWikiIcon() =>
        new(Options.Create(
            new PresentationOptions { RoleEmoji = { ["Wiki Editor"] = WikiIcon } }));
}
