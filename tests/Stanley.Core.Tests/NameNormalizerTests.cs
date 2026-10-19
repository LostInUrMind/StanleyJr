using Stanley.Core.Naming;

namespace Stanley.Core.Tests;

/// <summary>
/// FR-02: a name matches whatever the user typed, ignoring case, spaces, apostrophes and
/// punctuation. v1 could not find 23 of its 309 fighters by name at all, so these tests cover
/// the shapes that broke it.
/// </summary>
public class NameNormalizerTests
{
    [Theory]
    [InlineData("Assassin's Greed", "assassinsgreed")]
    [InlineData("assassins greed", "assassinsgreed")]
    [InlineData("ASSASSINSGREED", "assassinsgreed")]
    [InlineData("  Assassin's   Greed  ", "assassinsgreed")]
    public void The_acceptance_criteria_of_FR_02_all_reach_one_slug(string input, string expected)
    {
        Assert.Equal(expected, NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("Heat Synced", "heatsynced")]
    [InlineData("Blitz & Glamour", "blitzglamour")]
    [InlineData("Locked 'N' Loaded", "lockednloaded")]
    [InlineData("G.I. Jazz", "gijazz")]
    [InlineData("Robo-Fortune", "robofortune")]
    [InlineData("M-3OW", "m3ow")]
    [InlineData("Jack-O-Specter", "jackospecter")]
    [InlineData("Ms. Fortune", "msfortune")]
    public void Punctuation_and_spacing_are_dropped(string input, string expected)
    {
        Assert.Equal(expected, NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("Très Chic")]
    [InlineData("Tres Chic")]
    [InlineData("tres chic")]
    [InlineData("TRÈS CHIC")]
    public void An_accent_does_not_have_to_be_typed(string input)
    {
        // Folding the accent is what lets a player find this fighter without a French keyboard.
        Assert.Equal("treschic", NameNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("---")]
    public void Input_with_nothing_to_match_on_is_empty_and_not_normalizable(string? input)
    {
        Assert.Equal(string.Empty, NameNormalizer.Normalize(input));
        Assert.False(NameNormalizer.IsNormalizable(input));
    }

    [Fact]
    public void Every_legacy_fighter_normalizes_to_the_slug_it_was_imported_with()
    {
        var mismatches = new List<string>();

        foreach (GoldenFighter fighter in GoldenFighters.All)
        {
            string slug = NameNormalizer.Normalize(fighter.Name);

            if (slug != fighter.Slug)
            {
                mismatches.Add($"{fighter.Name}: imported as '{fighter.Slug}', normalizes to '{slug}'");
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void Every_legacy_fighter_name_normalizes_to_its_own_slug()
    {
        // variants.slug is unique, so the import would fail outright on a collision. This says
        // which names collided instead of leaving it to a constraint violation.
        var collisions = GoldenFighters.All
            .GroupBy(f => NameNormalizer.Normalize(f.Name))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(f => f.Name))}")
            .ToList();

        Assert.Empty(collisions);
        Assert.Equal(309, GoldenFighters.All.Count);
    }
}
