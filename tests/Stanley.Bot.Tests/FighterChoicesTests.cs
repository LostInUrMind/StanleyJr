using Stanley.Bot.Modules;
using Stanley.Data;

namespace Stanley.Bot.Tests;

/// <summary>
/// The fixed lists FR-13 requires for rarity and element are code, while the rows they name are
/// data. This is what keeps the two from drifting apart.
/// </summary>
/// <remarks>
/// A mismatch would not corrupt anything — the repository matches the name against the table
/// and refuses one it cannot find — but it would refuse a choice the editor was offered, which
/// is a bug the editor cannot do anything about. The order matters too: it is the order the
/// choices are listed in.
/// </remarks>
public class FighterChoicesTests
{
    [Fact]
    public void The_rarity_choices_are_the_seeded_tiers_in_order()
    {
        Assert.Equal(SeedData.Tiers.Select(tier => tier.Name), Enum.GetNames<FighterRarity>());
    }

    [Fact]
    public void The_element_choices_are_the_seeded_elements_in_order()
    {
        Assert.Equal(
            SeedData.Elements.Select(element => element.Name),
            Enum.GetNames<FighterElement>());
    }
}
