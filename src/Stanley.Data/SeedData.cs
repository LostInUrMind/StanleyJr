using Stanley.Data.Entities;

namespace Stanley.Data;

/// <summary>
/// The reference rows written by the first migration (database design sections 5.1 and 5.2).
/// </summary>
/// <remarks>
/// They are seeded by the migration rather than by a script so a fresh database and a restored
/// backup agree. Tests read the same arrays, so the seed and its expectations cannot drift.
/// </remarks>
public static class SeedData
{
    /// <summary>
    /// The four tiers. <c>EvoReq</c> is how many duplicates merge into the next tier;
    /// <c>EvolutionsToMax</c> is the different quantity the stat formula uses.
    /// </summary>
    public static IReadOnlyList<Tier> Tiers { get; } =
    [
        new() { Id = 1, Name = "Bronze", EvolutionsToMax = 3, EvoReq = 3, EvoReqSpecial = false },
        new() { Id = 2, Name = "Silver", EvolutionsToMax = 2, EvoReq = 4, EvoReqSpecial = false },
        new()
        {
            Id = 3,
            Name = "Gold",
            EvolutionsToMax = 1,
            EvoReq = 5,
            EvoReqSpecial = true,
            // The Neutral exception (one Air, one Water and one Fire essence) depends on the
            // element as well as the tier, so it cannot be stated here. element_tier_rel states
            // the whole rule in eight rows and replaces this column in phase 2.
            EvoSpecialMat = "Three essences matching the variant's element",
        },
        // Diamond is the top tier, so it has no merge cost and needs no further evolution.
        new() { Id = 4, Name = "Diamond", EvolutionsToMax = 0 },
    ];

    /// <summary>The six elements, with the card side-strip colours from FRD Table A-1.</summary>
    public static IReadOnlyList<Element> Elements { get; } =
    [
        new() { Id = 1, Name = "Fire", ColorHex = "#E74C3C" },
        new() { Id = 2, Name = "Water", ColorHex = "#3498DB" },
        new() { Id = 3, Name = "Air", ColorHex = "#2ECC71" },
        new() { Id = 4, Name = "Light", ColorHex = "#FFFF82" },
        new() { Id = 5, Name = "Dark", ColorHex = "#9B59B6" },
        new() { Id = 6, Name = "Neutral", ColorHex = "#FF6EC7" },
    ];
}
