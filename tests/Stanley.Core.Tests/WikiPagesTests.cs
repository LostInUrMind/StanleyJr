using Stanley.Core.Wiki;

namespace Stanley.Core.Tests;

/// <summary>
/// The wiki URL rules (FR-07) and the pages that are not fighters (FR-08).
/// </summary>
public class WikiPagesTests
{
    [Fact]
    public void Fighter_url_turns_spaces_into_underscores()
    {
        // FR-07's acceptance criterion, verbatim.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Vintage_Virtuoso",
            WikiPages.UrlForTitle("Vintage Virtuoso"));
    }

    [Fact]
    public void Fighter_url_keeps_an_apostrophe()
    {
        // What v1 sent, and a working wiki URL. Escaping it would be a change in behaviour.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Assassin's_Greed",
            WikiPages.UrlForTitle("Assassin's Greed"));
    }

    [Fact]
    public void Stored_url_overrides_the_built_one()
    {
        // Why variants.wiki_url exists: a page whose title is not the fighter's name.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Something_Else",
            WikiPages.UrlFor(
                "Vintage Virtuoso",
                "https://skullgirlsmobile.fandom.com/wiki/Something_Else"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Absent_stored_url_falls_back_to_the_built_one(string? stored)
    {
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Vintage_Virtuoso",
            WikiPages.UrlFor("Vintage Virtuoso", stored));
    }

    [Theory]
    [InlineData("critless")]
    [InlineData("Critless Skill Tree")]
    [InlineData("critlessskilltree")]
    [InlineData("CRITLESS")]
    public void Critless_resolves_from_every_name_v1_accepted(string input)
    {
        WikiExtraPage? page = WikiPages.Find(input);

        // FR-08's acceptance criterion.
        Assert.NotNull(page);
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Critless_Skill_Tree",
            page.Url);
    }

    [Theory]
    [InlineData("fortunesfavor")]
    [InlineData("Fortune's Favor")]
    [InlineData("fortune'sfavor")]
    public void Fortunes_favor_resolves_from_every_name_v1_accepted(string input)
    {
        WikiExtraPage? page = WikiPages.Find(input);

        Assert.NotNull(page);
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Fortune's_Favor",
            page.Url);
    }

    [Theory]
    [InlineData("Vintage Virtuoso")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void Anything_else_is_not_an_extra_page(string input)
    {
        // A fighter's name must fall through to the database, and an input that normalizes to
        // nothing must not match the first page in the list.
        Assert.Null(WikiPages.Find(input));
    }

    [Fact]
    public void Empty_input_suggests_every_extra_page()
    {
        Assert.Equal(WikiPages.Extras, WikiPages.Suggest(string.Empty));
    }

    [Theory]
    [InlineData("crit", "Critless Skill Tree")]
    [InlineData("skill", "Critless Skill Tree")]
    [InlineData("fortune", "Fortune's Favor")]
    public void Partly_typed_names_suggest_the_page(string typed, string expected)
    {
        WikiExtraPage page = Assert.Single(WikiPages.Suggest(typed));

        Assert.Equal(expected, page.Title);
    }

    [Fact]
    public void A_fighter_name_suggests_no_extra_page()
    {
        Assert.Empty(WikiPages.Suggest("Vintage"));
    }

    [Fact]
    public void Suggestion_values_resolve_back_to_the_page_they_came_from()
    {
        // What autocomplete sends back has to be something Find understands, or picking a
        // suggestion would fail.
        foreach (WikiExtraPage page in WikiPages.Extras)
        {
            Assert.Same(page, WikiPages.Find(page.Slug));
        }
    }
}
