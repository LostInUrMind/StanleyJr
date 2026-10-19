using Stanley.Core.Naming;
using Stanley.Data.Entities;

namespace Stanley.Data.Tests;

/// <summary>
/// Builds valid rows for the tests, so each test only spells out the part it is about.
/// </summary>
internal static class TestFighters
{
    /// <summary>
    /// A variant with its own character, attached to seeded tier and element rows. Names carry
    /// a unique suffix because <c>variants.name</c> and <c>variants.slug</c> are unique and the
    /// tests share one database.
    /// </summary>
    public static Variant NewVariant(
        short tierId = 4,
        short elementId = 5,
        bool isRaidBoss = false,
        int abilities = 2)
    {
        string unique = Guid.NewGuid().ToString("N")[..8];
        string name = $"Test Fighter {unique}";

        var variant = new Variant
        {
            Name = name,
            Slug = NameNormalizer.Normalize(name),
            SaName = "TEST ABILITY",
            Character = new Character { Name = $"Test Character {unique}" },
            TierId = tierId,
            ElementId = elementId,
            BaseAtk = 617,
            BaseHp = 4169,
            BaseFs = 919,
            IsRaidBoss = isRaidBoss,
        };

        for (int i = 1; i <= abilities; i++)
        {
            variant.SignatureAbilities.Add(new SignatureAbility
            {
                Ordinal = (short)i,
                AbilityText = $"Test ability {i}.",
            });
        }

        return variant;
    }

    /// <summary>An alias of <paramref name="variant"/>, normalized the way a command would.</summary>
    public static VariantAlias NewAlias(Variant variant, string alias) => new()
    {
        Variant = variant,
        Alias = alias,
        AliasSlug = NameNormalizer.Normalize(alias),
    };
}
