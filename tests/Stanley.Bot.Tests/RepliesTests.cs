using Stanley.Bot.Presentation;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Tests;

/// <summary>
/// The wording the FRD states word for word, read from the one place that holds it
/// rather than copied into the test.
/// </summary>
public class RepliesTests
{
    [Fact]
    public void A_fighter_with_no_aliases_is_said_to_have_none()
    {
        Assert.Equal("Heat Synced has no aliases.", Replies.AliasList("Heat Synced", []));
    }

    [Theory]
    // One alias, two joined by "and", and three with the serial comma the FRD uses.
    [InlineData("Heat Synced is also known as heat.", "heat")]
    [InlineData("Heat Synced is also known as heat and hs.", "heat", "hs")]
    [InlineData("Heat Synced is also known as heat, hs, and synced.", "heat", "hs", "synced")]
    public void A_fighters_aliases_are_read_out_as_a_sentence(string expected, params string[] aliases)
    {
        Assert.Equal(expected, Replies.AliasList("Heat Synced", aliases));
    }

    [Fact]
    public void A_refused_alias_names_the_fighter_that_already_uses_it()
    {
        // The refusal has to say which fighter the alias is taken by, whether it is
        // that fighter's alias or its own name.
        Assert.Contains("Heat Synced", Replies.AliasTaken("heat", "Heat Synced"), StringComparison.Ordinal);
        Assert.Contains(
            "Heat Synced",
            Replies.AliasIsFighterName("Heat Synced", "Heat Synced"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_fighter_with_more_aliases_than_a_message_holds_lists_the_rest_as_a_count()
    {
        // 100 characters is the longest alias /alias-admin add accepts, so 40 of them cannot fit in
        // one message. The reply has to fit anyway (the FRD states the wording, not a limit).
        string[] aliases = [.. Enumerable.Range(0, 40).Select(i => $"{i}".PadRight(100, 'a'))];

        string reply = Replies.AliasList("Heat Synced", aliases);

        Assert.True(reply.Length <= Replies.MaxMessageLength);
        Assert.Contains(" more.", reply, StringComparison.Ordinal);
        Assert.StartsWith("Heat Synced is also known as ", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_choice_never_exceeds_what_Discord_displays()
    {
        var suggestion = new Data.Aliases.AliasSuggestion(
            new string('a', 80),
            "aaa",
            new string('b', 80));

        Assert.Equal(Replies.MaxChoiceLength, Replies.ChoiceLabel(suggestion).Length);
    }

    [Fact]
    public void A_wiki_miss_mentions_pages_as_well_as_fighters()
    {
        // /wiki answers for the pages that are not fighters too, so "no fighter matches"
        // would send the user looking for a fighter they never asked for.
        string reply = Replies.NoWikiMatch("critles");

        Assert.Contains("wiki page", reply, StringComparison.Ordinal);
        Assert.Contains("critles", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wiki_page_choice_drops_the_wikis_own_prefix()
    {
        // What is left is the part that differs, so two keys for one page read alike in the
        // editor's list.
        string label = Replies.ChoiceLabel(new WikiPageSuggestion(
            "critless",
            "critless",
            "https://skullgirlsmobile.fandom.com/wiki/Critless_Skill_Tree"));

        Assert.Equal("critless → Critless_Skill_Tree", label);
    }

    [Fact]
    public void A_wiki_page_choice_elsewhere_shows_the_whole_address()
    {
        string label = Replies.ChoiceLabel(
            new WikiPageSuggestion("elsewhere", "elsewhere", "https://example.com/page"));

        Assert.Equal("elsewhere → https://example.com/page", label);
    }

    [Fact]
    public void A_wiki_page_choice_is_never_longer_than_discord_shows()
    {
        string label = Replies.ChoiceLabel(new WikiPageSuggestion(
            new string('k', 100),
            "k",
            "https://example.com/" + new string('p', 400)));

        Assert.Equal(Replies.MaxChoiceLength, label.Length);
    }
}
