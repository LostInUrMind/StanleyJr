using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Stanley.Data.Entities;
using Stanley.Data.Views;

namespace Stanley.Data.Tests;

/// <summary>
/// What the first migration must produce: the seeded reference rows, the extension, the three
/// views, and the constraints the editor commands rely on to refuse bad data.
/// </summary>
/// <remarks>
/// These are the phase 3 checks of the migration plan, run against a real PostgreSQL rather
/// than asserted against the model.
/// </remarks>
[Collection(PostgresCollection.Name)]
[Trait("RequiresDocker", "true")]
public class SchemaTests(PostgresFixture postgres)
{
    [Fact]
    public async Task The_migration_seeds_the_four_tiers()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        List<Tier> tiers = await db.Tiers.OrderBy(t => t.Id).ToListAsync();

        Assert.Equal(4, tiers.Count);
        Assert.Equal(
            SeedData.Tiers.Select(t => (t.Name, t.EvolutionsToMax)),
            tiers.Select(t => (t.Name, t.EvolutionsToMax)));
    }

    [Fact]
    public async Task The_migration_seeds_the_six_elements_with_their_card_colours()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        List<Element> elements = await db.Elements.OrderBy(e => e.Id).ToListAsync();

        Assert.Equal(6, elements.Count);
        Assert.Equal(
            SeedData.Elements.Select(e => (e.Name, e.ColorHex)),
            elements.Select(e => (e.Name, e.ColorHex)));

        // FRD Table A-1, spelled out so a mistyped seed fails here and not on a card.
        Assert.Equal("#9B59B6", elements.Single(e => e.Name == "Dark").ColorHex);
    }

    [Fact]
    public async Task The_trigram_extension_is_installed()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        int installed = await db.Database
            .SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_extension WHERE extname = 'pg_trgm'")
            .CountAsync();

        Assert.Equal(1, installed);
    }

    [Fact]
    public async Task The_count_views_leave_raid_bosses_out()
    {
        const short tierId = 4;   // Diamond, which is what the three Raid Bosses are.
        const short elementId = 1; // Fire

        await using StanleyDbContext db = postgres.CreateContext();

        long tierBefore = await TierCountAsync(db, tierId);
        long elementBefore = await ElementCountAsync(db, elementId);

        db.Variants.Add(TestFighters.NewVariant(tierId, elementId));
        db.Variants.Add(TestFighters.NewVariant(tierId, elementId));
        db.Variants.Add(TestFighters.NewVariant(tierId, elementId, isRaidBoss: true, abilities: 5));
        await db.SaveChangesAsync();

        // Three fighters were added and the Raid Boss is not collectible, so both counts move
        // by two (database design section 6.1).
        Assert.Equal(tierBefore + 2, await TierCountAsync(db, tierId));
        Assert.Equal(elementBefore + 2, await ElementCountAsync(db, elementId));
    }

    [Fact]
    public async Task Every_tier_and_element_appears_in_its_count_view()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        // A LEFT JOIN, so a tier with nothing countable still returns a row with zero.
        Assert.Equal(4, await db.TierVariantCounts.CountAsync());
        Assert.Equal(6, await db.ElementVariantCounts.CountAsync());
    }

    [Fact]
    public async Task Searchable_names_covers_both_names_and_aliases()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant();
        db.Variants.Add(variant);
        db.VariantAliases.Add(TestFighters.NewAlias(variant, "The Nickname"));
        await db.SaveChangesAsync();

        List<SearchableName> found = await db.SearchableNames
            .Where(n => n.VariantId == variant.Id)
            .ToListAsync();

        Assert.Equal(2, found.Count);
        Assert.Contains(found, n => n.Kind == "name" && n.Slug == variant.Slug);
        Assert.Contains(found, n => n.Kind == "alias" && n.Slug == "thenickname");
    }

    [Fact]
    public async Task A_raid_boss_is_still_searchable()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant raidBoss = TestFighters.NewVariant(isRaidBoss: true, abilities: 5);
        db.Variants.Add(raidBoss);
        await db.SaveChangesAsync();

        Assert.Contains(
            await db.SearchableNames.Where(n => n.VariantId == raidBoss.Id).ToListAsync(),
            n => n.Slug == raidBoss.Slug);
    }

    [Theory]
    [InlineData(0, 4169, 919)]
    [InlineData(617, 0, 919)]
    [InlineData(617, 4169, 0)]
    [InlineData(-1, 4169, 919)]
    public async Task A_stat_of_zero_or_less_is_refused(int atk, int hp, int fs)
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant();
        variant.BaseAtk = atk;
        variant.BaseHp = hp;
        variant.BaseFs = fs;
        db.Variants.Add(variant);

        // FR-13: stats must be whole numbers greater than zero.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task An_ability_longer_than_a_discord_embed_field_is_refused()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant(abilities: 0);
        variant.SignatureAbilities.Add(new SignatureAbility
        {
            Ordinal = 1,
            AbilityText = new string('a', SignatureAbility.MaxTextLength + 1),
        });
        db.Variants.Add(variant);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task An_ability_of_exactly_the_embed_field_limit_is_accepted()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant(abilities: 0);
        variant.SignatureAbilities.Add(new SignatureAbility
        {
            Ordinal = 1,
            AbilityText = new string('a', SignatureAbility.MaxTextLength),
        });
        db.Variants.Add(variant);

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.SignatureAbilities.CountAsync(a => a.VariantId == variant.Id));
    }

    [Fact]
    public async Task An_ability_numbered_below_one_is_refused()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant(abilities: 0);
        variant.SignatureAbilities.Add(new SignatureAbility
        {
            Ordinal = 0,
            AbilityText = "Ordinals start at 1.",
        });
        db.Variants.Add(variant);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task One_fighter_cannot_have_two_abilities_with_the_same_number()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant();
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        await using StanleyDbContext second = postgres.CreateContext();
        second.SignatureAbilities.Add(new SignatureAbility
        {
            VariantId = variant.Id,
            Ordinal = 1,
            AbilityText = "A second SA1.",
        });

        // The primary key is (variant_id, ordinal), which is what guarantees it.
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Removing_a_fighter_removes_its_abilities_and_aliases()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant(abilities: 5);
        db.Variants.Add(variant);
        db.VariantAliases.Add(TestFighters.NewAlias(variant, $"Alias {Guid.NewGuid():N}"));
        await db.SaveChangesAsync();

        int variantId = variant.Id;

        db.Variants.Remove(variant);
        await db.SaveChangesAsync();

        // FR-14: the fighter's aliases are removed with it, and so are its abilities.
        Assert.Equal(0, await db.SignatureAbilities.CountAsync(a => a.VariantId == variantId));
        Assert.Equal(0, await db.VariantAliases.CountAsync(a => a.VariantId == variantId));
    }

    [Fact]
    public async Task A_character_that_still_has_fighters_cannot_be_deleted()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant();
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        await using StanleyDbContext second = postgres.CreateContext();
        Character character = await second.Characters.SingleAsync(c => c.Id == variant.CharacterId);
        second.Characters.Remove(character);

        // Restrict: the variants have to go first.
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task The_same_alias_cannot_be_added_twice()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant first = TestFighters.NewVariant();
        Variant second = TestFighters.NewVariant();
        db.Variants.AddRange(first, second);
        await db.SaveChangesAsync();

        string alias = $"Shared {Guid.NewGuid():N}";
        db.VariantAliases.Add(TestFighters.NewAlias(first, alias));
        await db.SaveChangesAsync();

        db.VariantAliases.Add(TestFighters.NewAlias(second, alias));

        // FR-10: alias_slug is unique, so a duplicate cannot reach the table.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task An_element_colour_that_is_not_a_hex_triplet_is_refused()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        db.Elements.Add(new Element { Name = $"Test {Guid.NewGuid():N}", ColorHex = "purple" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_change_record_round_trips_its_action_and_entity_type()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        var record = new ChangeRecord
        {
            ActorUserId = 123456789012345678,
            GuildId = 987654321098765432,
            Action = ChangeAction.Update,
            EntityType = ChangeEntityType.SignatureAbility,
            EntityKey = "42:3",
            Before = """{"ability_text":"before"}""",
            After = """{"ability_text":"after"}""",
        };

        db.ChangeRecords.Add(record);
        await db.SaveChangesAsync();

        await using StanleyDbContext second = postgres.CreateContext();
        ChangeRecord stored = await second.ChangeRecords.SingleAsync(r => r.Id == record.Id);

        Assert.Equal(ChangeAction.Update, stored.Action);
        Assert.Equal(ChangeEntityType.SignatureAbility, stored.EntityType);
        Assert.Equal("42:3", stored.EntityKey);

        // A snowflake needs all 64 bits; this is why the column is bigint.
        Assert.Equal(123456789012345678, stored.ActorUserId);

        // The enums are stored as the strings the CHECK constraint allows, not as numbers.
        string action = await second.Database
            .SqlQuery<string>($"SELECT action AS \"Value\" FROM change_records WHERE id = {record.Id}")
            .SingleAsync();
        Assert.Equal("update", action);
    }

    [Fact]
    public async Task An_editors_id_can_be_cleared_without_losing_the_change()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        var record = new ChangeRecord
        {
            ActorUserId = 111222333444555666,
            GuildId = 666555444333222111,
            Action = ChangeAction.Insert,
            EntityType = ChangeEntityType.VariantAlias,
            EntityKey = "7",
            After = """{"alias":"nickname"}""",
        };
        db.ChangeRecords.Add(record);
        await db.SaveChangesAsync();

        // CR-02: a removal request clears the person, and the game data stays.
        record.ActorUserId = null;
        record.GuildId = null;
        await db.SaveChangesAsync();

        await using StanleyDbContext second = postgres.CreateContext();
        ChangeRecord stored = await second.ChangeRecords.SingleAsync(r => r.Id == record.Id);

        Assert.Null(stored.ActorUserId);
        Assert.Null(stored.GuildId);

        // jsonb is not text: PostgreSQL parses it and renders its own formatting on the
        // way out, so comparing characters fails on nothing more than a space after the
        // colon. Compare the documents instead.
        Assert.Equal(
            JsonNode.Parse("""{"alias":"nickname"}""")!.ToJsonString(),
            JsonNode.Parse(stored.After!)!.ToJsonString());
    }

    [Fact]
    public async Task Saving_sets_the_timestamps()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant variant = TestFighters.NewVariant();
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        Assert.NotEqual(default, variant.CreatedAt);
        DateTime created = variant.UpdatedAt;

        variant.Quote = "Changed.";
        await db.SaveChangesAsync();

        Assert.True(variant.UpdatedAt >= created);
        Assert.Equal(DateTimeKind.Utc, variant.UpdatedAt.Kind);
    }

    private static async Task<long> TierCountAsync(StanleyDbContext db, short tierId) =>
        (await db.TierVariantCounts.SingleAsync(t => t.Id == tierId)).TotalVariants;

    private static async Task<long> ElementCountAsync(StanleyDbContext db, short elementId) =>
        (await db.ElementVariantCounts.SingleAsync(e => e.Id == elementId)).TotalVariants;
}
