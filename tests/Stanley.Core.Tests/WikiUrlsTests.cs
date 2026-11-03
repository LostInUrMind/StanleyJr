using Stanley.Core.Wiki;

namespace Stanley.Core.Tests;

/// <summary>
/// The wiki URL rules for fighters.
/// </summary>
/// <remarks>
/// The wiki pages that are not fighters were a list beside this class until they became the
/// <c>wiki_pages</c> table; what they must do is now
/// <c>Stanley.Data.Tests.WikiPageRepositoryTests</c> and the seeding check in
/// <c>SchemaTests</c>.
/// </remarks>
public class WikiUrlsTests
{
    [Fact]
    public void Fighter_url_turns_spaces_into_underscores()
    {
        // The acceptance criterion, verbatim.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Vintage_Virtuoso",
            WikiUrls.UrlForTitle("Vintage Virtuoso"));
    }

    [Fact]
    public void Fighter_url_keeps_an_apostrophe()
    {
        // What v1 sent, and a working wiki URL. Escaping it would be a change in behaviour.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Assassin's_Greed",
            WikiUrls.UrlForTitle("Assassin's Greed"));
    }

    [Fact]
    public void Stored_url_overrides_the_built_one()
    {
        // Why variants.wiki_url exists: a page whose title is not the fighter's name.
        Assert.Equal(
            "https://skullgirlsmobile.fandom.com/wiki/Something_Else",
            WikiUrls.UrlFor(
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
            WikiUrls.UrlFor("Vintage Virtuoso", stored));
    }
}
