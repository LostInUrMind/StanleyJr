using Stanley.Core.Stats;
using Stanley.Data;
using Stanley.Data.Entities;

namespace Stanley.Core.Tests;

/// <summary>
/// The proof that the migration did not change what Stanley shows (FR-06).
/// </summary>
/// <remarks>
/// FR-06's acceptance criterion is "an automated test compares v2's results with v1's for all
/// 309 fighters and finds no difference". That is <see cref="Matches_v1_for_every_fighter"/>;
/// the rest guard the pieces it depends on.
/// </remarks>
public class StatCalculatorTests
{
    /// <summary>
    /// The seeded evolution count for each tier, which is the only place the formula's
    /// <c>evos</c> comes from (database design section 3.4).
    /// </summary>
    private static readonly Dictionary<string, short> Evolutions =
        SeedData.Tiers.ToDictionary(t => t.Name, t => t.EvolutionsToMax);

    [Fact]
    public void Matches_v1_for_every_fighter()
    {
        Assert.Equal(309, GoldenFighters.All.Count);

        var mismatches = new List<string>();

        foreach (GoldenFighter fighter in GoldenFighters.All)
        {
            MaxStats actual = StatCalculator.Calculate(
                fighter.BaseAtk,
                fighter.BaseHp,
                Evolutions[fighter.Tier]);

            var expected = new MaxStats(fighter.MaxAtk, fighter.MaxHp, fighter.MaxFs);

            if (actual != expected)
            {
                mismatches.Add($"{fighter.Name} ({fighter.Tier}): expected {expected}, got {actual}");
            }
        }

        Assert.Empty(mismatches);
    }

    /// <summary>
    /// The worked examples in FRD Table B-1, one per tier. They are spelled out so a failure
    /// points at the document rather than only at the fixture.
    /// </summary>
    [Theory]
    [InlineData("Bronze", 66, 450, 9428, 64090, 23762)]    // Beat Box
    [InlineData("Silver", 132, 809, 9620, 59079, 23002)]   // Blue Bomber
    [InlineData("Gold", 297, 1698, 11117, 63572, 25656)]   // Lucky Lure
    [InlineData("Diamond", 617, 4169, 11847, 80045, 29762)] // Brass Bandit
    public void Matches_the_worked_examples_of_the_requirements(
        string tier,
        int baseAtk,
        int baseHp,
        int expectedAtk,
        int expectedHp,
        int expectedFs)
    {
        MaxStats actual = StatCalculator.Calculate(baseAtk, baseHp, Evolutions[tier]);

        Assert.Equal(new MaxStats(expectedAtk, expectedHp, expectedFs), actual);
    }

    [Fact]
    public void Seeded_evolution_counts_match_the_requirements()
    {
        // FRD Table B-1. If a seed row is edited, the formula silently changes for a whole
        // tier, so the numbers are asserted here as well as used above.
        Assert.Equal(3, Evolutions["Bronze"]);
        Assert.Equal(2, Evolutions["Silver"]);
        Assert.Equal(1, Evolutions["Gold"]);
        Assert.Equal(0, Evolutions["Diamond"]);
    }

    [Fact]
    public void A_diamond_fighter_is_not_evolved_at_all()
    {
        // Diamond needs no evolutions, so its maximum is the base stat times 19.2, rounded up.
        MaxStats actual = StatCalculator.Calculate(617, 4169, evolutionsToMax: 0);

        Assert.Equal((int)Math.Ceiling(617 * 19.2), actual.Atk);
        Assert.Equal((int)Math.Ceiling(4169 * 19.2), actual.Hp);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(-1, 100, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(100, 100, -1)]
    [InlineData(100, 100, StatCalculator.MaxEvolutions + 1)]
    public void Refuses_values_the_database_would_also_refuse(
        int baseAtk,
        int baseHp,
        int evolutionsToMax)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StatCalculator.Calculate(baseAtk, baseHp, evolutionsToMax));
    }

    [Fact]
    public void Every_tier_in_the_fixture_is_a_seeded_tier()
    {
        IEnumerable<string> seeded = SeedData.Tiers.Select((Tier t) => t.Name);

        Assert.Empty(GoldenFighters.All.Select(f => f.Tier).Distinct().Except(seeded));
    }
}
