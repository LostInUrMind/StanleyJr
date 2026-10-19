namespace Stanley.Data.Entities;

/// <summary>
/// Bronze, Silver, Gold or Diamond: four seeded rows (database design section 3.4).
/// The FRD calls this a fighter's rarity.
/// </summary>
/// <remarks>
/// A table rather than a C# enum because it carries data of its own:
/// <see cref="EvolutionsToMax"/> drives the maximum-stat formula (FR-06).
/// </remarks>
public class Tier
{
    public short Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Evolutions needed to reach maximum level: Bronze 3, Silver 2, Gold 1, Diamond 0.
    /// Read by <see cref="Core.Stats.StatCalculator"/> and by nothing else. Also the display
    /// order, highest first.
    /// </summary>
    public short EvolutionsToMax { get; set; }

    /// <summary>
    /// Duplicates of this tier, besides the original, merged to reach the next tier:
    /// 3 Bronze, 4 Silver, 5 Gold. Droppable (database design Appendix C).
    /// </summary>
    public short? EvoReq { get; set; }

    /// <summary>
    /// Whether reaching the next tier also needs special materials. True for Gold.
    /// Droppable.
    /// </summary>
    public bool? EvoReqSpecial { get; set; }

    /// <summary>
    /// The special materials as text. Holds the general rule until <c>element_tier_rel</c>
    /// is built in phase 2, which can also express the Neutral exception. Droppable.
    /// </summary>
    public string? EvoSpecialMat { get; set; }

    public ICollection<Variant> Variants { get; } = [];
}
