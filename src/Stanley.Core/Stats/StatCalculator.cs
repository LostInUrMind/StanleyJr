namespace Stanley.Core.Stats;

/// <summary>
/// The v1 maximum-stat formula (FRD Appendix B), ported unchanged.
/// </summary>
/// <remarks>
/// <para>
/// Rounding is half-to-even, which is what Python's <c>round</c> does. C# rounds that way by
/// default too, but it is passed explicitly here so the rule survives a later edit.
/// </para>
/// </remarks>
public static class StatCalculator
{
    private const double EvolutionMultiplier = 1.95;

    private const double MaxLevelMultiplier = 19.2;

    private const double FighterScoreMultiplier = 1.688;

    public const int MaxEvolutions = 3;

    /// <summary>
    /// Calculates maximum ATK, HP and FS from a fighter's base stats and its tier's
    /// <c>evolutions_to_max</c> (Bronze 3, Silver 2, Gold 1, Diamond 0).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A base stat is not greater than zero, or the evolution count is outside 0 to <see cref="MaxEvolutions"/>.
    /// </exception>
    public static MaxStats Calculate(int baseAtk, int baseHp, int evolutionsToMax)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseAtk);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseHp);
        ArgumentOutOfRangeException.ThrowIfNegative(evolutionsToMax);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(evolutionsToMax, MaxEvolutions);

        int maxAtk = (int)Math.Ceiling(Evolve(baseAtk, evolutionsToMax) * MaxLevelMultiplier);
        int maxHp = (int)Math.Ceiling(Evolve(baseHp, evolutionsToMax) * MaxLevelMultiplier);

        // v1 rounds to 3 decimals here before the last multiplication. Dropping that round
        // changes the result for some fighters, so it stays.
        double score = Math.Round((6d * maxAtk + maxHp) * 7d / 60d, 3, MidpointRounding.ToEven);
        int maxFs = (int)Math.Ceiling(score * FighterScoreMultiplier);

        return new MaxStats(maxAtk, maxHp, maxFs);
    }

    /// <summary>
    /// Applies one evolution at a time, rounding after each one. The repeated rounding is part
    /// of the formula: evolving once by 1.95^n gives different numbers.
    /// </summary>
    private static double Evolve(int baseStat, int evolutions)
    {
        double stat = baseStat;

        for (int i = 0; i < evolutions; i++)
        {
            stat = Math.Round(stat * EvolutionMultiplier, MidpointRounding.ToEven);
        }

        return stat;
    }
}
