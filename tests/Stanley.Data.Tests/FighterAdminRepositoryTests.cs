using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Stanley.Core.Naming;
using Stanley.Data.Aliases;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Fighters;
using Stanley.Data.Lookup;

namespace Stanley.Data.Tests;

/// <summary>
/// What <c>/fighter-admin</c> must do: add a fighter that can be read back at once (FR-12),
/// change the fields an editor gave and refuse the values the rules forbid (FR-13), keep the SA
/// numbers contiguous from 1 (FR-13), remove a fighter with everything that belongs to it
/// (FR-14), and record every write (FR-15).
/// </summary>
/// <remarks>
/// Against a real PostgreSQL, because the parts being tested are the unique indexes, the stat
/// check constraints, the cascades and the <c>searchable_names</c> view. The tests share one
/// database, so every fighter, character and alias carries a unique suffix and nothing asserts
/// on totals.
/// </remarks>
[Collection(PostgresCollection.Name)]
[Trait("RequiresDocker", "true")]
public class FighterAdminRepositoryTests(PostgresFixture postgres)
{
    /// <summary>The editor every write in these tests is attributed to (FR-15).</summary>
    private static readonly EditorContext Editor = new(ActorUserId: 777, GuildId: 99);

    /// <summary>Diamond and Dark: two of the rows the first migration seeds.</summary>
    private const string Rarity = "Diamond";

    private const string Element = "Dark";

    private FighterAdminRepository Fighters => new(new TestContextFactory(postgres));

    private FighterLookup Lookup => new(new TestContextFactory(postgres));

    private AliasRepository Aliases => new(new TestContextFactory(postgres));

    [Fact]
    public async Task An_added_fighter_can_be_looked_up_straight_away()
    {
        Character character = await AddCharacterAsync();
        string name = Unique("Heat Synced");

        FighterWriteResult added = await Fighters.AddAsync(
            NewDraft(name, character.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, added.Outcome);
        Assert.Equal(name, added.FighterName);

        // FR-12: without restarting the bot, and through the same lookup /fighter uses.
        FighterCard? card = await Lookup.FindCardAsync(name);

        Assert.NotNull(card);
        Assert.Equal(name, card.Name);
        Assert.Equal(character.Name, card.CharacterName);
        Assert.Equal(Rarity, card.TierName);
        Assert.Equal(Element, card.ElementName);
        Assert.Equal(617, card.BaseAtk);

        // Abilities come afterwards, one at a time (FR-13), so a new fighter has none.
        Assert.Empty(card.Abilities);
    }

    [Fact]
    public async Task Adding_a_fighter_appends_one_insert_change_record()
    {
        Character character = await AddCharacterAsync();
        string name = Unique("Recorded Fighter");

        await Fighters.AddAsync(NewDraft(name, character.Name), Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        Variant stored = await db.Variants.SingleAsync(variant => variant.Name == name);

        ChangeRecord record = await SingleRecordAsync(
            db,
            ChangeEntityType.Variant,
            Key(stored.Id));

        Assert.Equal(ChangeAction.Insert, record.Action);
        Assert.Equal(Editor.ActorUserId, record.ActorUserId);
        Assert.Equal(Editor.GuildId, record.GuildId);

        // An insert has no "before": there was no row (FR-15).
        Assert.Null(record.Before);

        JsonNode after = JsonNode.Parse(record.After!)!;
        Assert.Equal(name, (string?)after["name"]);
        Assert.Equal(NameNormalizer.Normalize(name), (string?)after["slug"]);
        Assert.Equal(617, (int?)after["baseAtk"]);
    }

    [Fact]
    public async Task A_name_another_fighter_already_has_is_refused_naming_that_fighter()
    {
        Character character = await AddCharacterAsync();
        Variant existing = await AddFighterAsync();

        FighterWriteResult refused = await Fighters.AddAsync(
            NewDraft(existing.Name, character.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.NameTaken, refused.Outcome);
        Assert.Equal(existing.Name, refused.Detail);
    }

    [Fact]
    public async Task A_name_an_alias_already_uses_is_refused_naming_its_fighter()
    {
        Character character = await AddCharacterAsync();
        Variant owner = await AddFighterAsync();
        string alias = Unique("Hairball");

        await Aliases.AddAsync(owner.Name, alias, Editor);

        // Names and aliases are looked up together, so a new fighter cannot take a name that
        // already finds another one (database design section 3.2).
        FighterWriteResult refused = await Fighters.AddAsync(
            NewDraft(alias, character.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.NameTaken, refused.Outcome);
        Assert.Equal(owner.Name, refused.Detail);
    }

    [Fact]
    public async Task A_name_of_punctuation_alone_is_refused()
    {
        Character character = await AddCharacterAsync();

        FighterWriteResult refused = await Fighters.AddAsync(
            NewDraft("!!! ???", character.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.NameNotUsable, refused.Outcome);
        Assert.Equal(FighterField.Name, refused.Field);
    }

    [Theory]
    [InlineData("Nobody Of That Name", Rarity, Element, FighterField.Character)]
    [InlineData(null, "Platinum", Element, FighterField.Rarity)]
    [InlineData(null, Rarity, "Plasma", FighterField.Element)]
    public async Task A_character_rarity_or_element_that_is_not_a_row_is_refused(
        string? character,
        string rarity,
        string element,
        FighterField expected)
    {
        Character existing = await AddCharacterAsync();

        FighterWriteResult refused = await Fighters.AddAsync(
            NewDraft(Unique("Unrooted"), character ?? existing.Name, rarity, element),
            Editor);

        // FR-13: the refusal states the rule that was broken, which here is the list the value
        // had to come from.
        Assert.Equal(FighterWriteOutcome.ValueNotAllowed, refused.Outcome);
        Assert.Equal(expected, refused.Field);
    }

    [Fact]
    public async Task A_stat_of_zero_and_a_portrait_that_is_not_a_url_are_refused()
    {
        Character character = await AddCharacterAsync();
        string name = Unique("Invalid");

        FighterWriteResult zeroAttack = await Fighters.AddAsync(
            NewDraft(name, character.Name) with { BaseAtk = 0 },
            Editor);

        Assert.Equal(FighterWriteOutcome.ValueNotPositive, zeroAttack.Outcome);
        Assert.Equal(FighterField.BaseAtk, zeroAttack.Field);

        FighterWriteResult notAUrl = await Fighters.AddAsync(
            NewDraft(name, character.Name) with { PortraitUrl = "portrait.png" },
            Editor);

        Assert.Equal(FighterWriteOutcome.ValueNotAUrl, notAUrl.Outcome);
        Assert.Equal(FighterField.PortraitUrl, notAUrl.Field);

        // Refused means refused: neither attempt left a row behind.
        await using StanleyDbContext db = postgres.CreateContext();
        Assert.False(await db.Variants.AnyAsync(variant => variant.Name == name));
    }

    [Fact]
    public async Task An_edit_changes_only_the_fields_given_and_reports_them_old_and_new()
    {
        Variant fighter = await AddFighterAsync();

        FighterWriteResult edited = await Fighters.EditAsync(
            fighter.Name,
            new FighterEdit(BaseAtk: 1234, Quote: "Say hello to my little friend."),
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, edited.Outcome);

        // FR-13 and FRD section 6.1: the confirmation shows old and new values, and only for
        // what changed.
        FighterFieldChange attack = Assert.Single(
            edited.Changes,
            change => change.Field == FighterField.BaseAtk);

        Assert.Equal("617", attack.Before);
        Assert.Equal("1234", attack.After);

        FighterFieldChange quote = Assert.Single(
            edited.Changes,
            change => change.Field == FighterField.Quote);

        Assert.Null(quote.Before);

        await using StanleyDbContext db = postgres.CreateContext();
        Variant stored = await db.Variants.SingleAsync(variant => variant.Id == fighter.Id);

        Assert.Equal(1234, stored.BaseAtk);
        Assert.Equal("Say hello to my little friend.", stored.Quote);

        // Untouched, because they were not given.
        Assert.Equal(fighter.BaseHp, stored.BaseHp);
        Assert.Equal(fighter.Name, stored.Name);
    }

    [Fact]
    public async Task An_edit_that_matches_what_is_stored_changes_nothing()
    {
        Variant fighter = await AddFighterAsync();

        FighterWriteResult edited = await Fighters.EditAsync(
            fighter.Name,
            new FighterEdit(Rarity: Rarity, BaseHp: fighter.BaseHp),
            Editor);

        Assert.Equal(FighterWriteOutcome.NothingToChange, edited.Outcome);
        Assert.Equal(fighter.Name, edited.FighterName);
        Assert.Empty(edited.Changes);

        // Nothing changed, so nothing was recorded either (FR-15).
        await using StanleyDbContext db = postgres.CreateContext();
        string key = Key(fighter.Id);

        Assert.False(await db.ChangeRecords.AnyAsync(record =>
            record.EntityType == ChangeEntityType.Variant && record.EntityKey == key));
    }

    [Fact]
    public async Task A_renamed_fighter_is_found_by_its_new_name_and_by_its_aliases()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Nickname");
        string newName = Unique("Renamed");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        FighterWriteResult edited = await Fighters.EditAsync(
            fighter.Name,
            new FighterEdit(Name: newName),
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, edited.Outcome);
        Assert.Equal(newName, edited.FighterName);

        await using StanleyDbContext db = postgres.CreateContext();
        Variant stored = await db.Variants.SingleAsync(variant => variant.Id == fighter.Id);

        // The slug is never set by hand, so a rename has to carry it (FR-02).
        Assert.Equal(NameNormalizer.Normalize(newName), stored.Slug);

        FighterCard? byNewName = await Lookup.FindCardAsync(newName);
        Assert.NotNull(byNewName);
        Assert.Equal(newName, byNewName.Name);

        // The aliases belong to the fighter, not to the name it had (FR-14 removes them with
        // the fighter; a rename keeps them).
        FighterCard? byAlias = await Lookup.FindCardAsync(alias);
        Assert.NotNull(byAlias);
        Assert.Equal(newName, byAlias.Name);
    }

    [Fact]
    public async Task A_rename_to_a_name_another_fighter_has_is_refused()
    {
        Variant fighter = await AddFighterAsync();
        Variant other = await AddFighterAsync();

        FighterWriteResult refused = await Fighters.EditAsync(
            fighter.Name,
            new FighterEdit(Name: other.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.NameTaken, refused.Outcome);
        Assert.Equal(other.Name, refused.Detail);

        // And renaming a fighter to the name it already has is not a clash with itself.
        FighterWriteResult itself = await Fighters.EditAsync(
            fighter.Name,
            new FighterEdit(Name: fighter.Name),
            Editor);

        Assert.Equal(FighterWriteOutcome.NothingToChange, itself.Outcome);
    }

    [Fact]
    public async Task Editing_appends_one_update_change_record_holding_both_states()
    {
        Variant fighter = await AddFighterAsync();

        await Fighters.EditAsync(fighter.Name, new FighterEdit(BaseFs: 2000), Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        ChangeRecord record = await SingleRecordAsync(
            db,
            ChangeEntityType.Variant,
            Key(fighter.Id));

        Assert.Equal(ChangeAction.Update, record.Action);

        JsonNode before = JsonNode.Parse(record.Before!)!;
        JsonNode after = JsonNode.Parse(record.After!)!;

        Assert.Equal(fighter.BaseFs, (int?)before["baseFs"]);
        Assert.Equal(2000, (int?)after["baseFs"]);

        // The record's "after" is the row as the edit left it, timestamp included, which is
        // why it is built after the save. Compared loosely because the column keeps
        // microseconds and a .NET timestamp has finer ticks than that.
        Variant stored = await db.Variants.SingleAsync(variant => variant.Id == fighter.Id);
        var recorded = (DateTime)after["updatedAt"]!;

        Assert.True(
            (stored.UpdatedAt - recorded).Duration() < TimeSpan.FromMilliseconds(1),
            $"recorded {recorded:O} is not the stored {stored.UpdatedAt:O}");
    }

    [Fact]
    public async Task An_ability_with_no_number_is_the_next_one_after_the_block()
    {
        Variant fighter = await AddFighterAsync();

        AbilityPrompt prompt = await Fighters.PrepareAbilityAsync(fighter.Name, null);

        Assert.Equal(FighterWriteOutcome.Ok, prompt.Outcome);
        Assert.Equal(fighter.Name, prompt.FighterName);

        // The test fighter has SA1 and SA2, so the next one is SA3 (database design 3.3).
        Assert.Equal(3, prompt.Ordinal);
        Assert.Equal(2, prompt.AbilityCount);

        // Nothing to pre-fill: this ability does not exist yet.
        Assert.Null(prompt.CurrentText);
    }

    [Fact]
    public async Task An_ability_that_exists_is_offered_with_its_current_text()
    {
        Variant fighter = await AddFighterAsync();

        AbilityPrompt prompt = await Fighters.PrepareAbilityAsync(fighter.Name, 1);

        Assert.Equal(FighterWriteOutcome.Ok, prompt.Outcome);
        Assert.Equal(1, prompt.Ordinal);

        // FR-13: the form opens pre-filled with the current text, so an edit is an edit.
        Assert.Equal("Test ability 1.", prompt.CurrentText);
    }

    [Fact]
    public async Task An_sa_number_past_the_end_of_the_block_is_refused()
    {
        Variant fighter = await AddFighterAsync();

        // SA4 for a fighter with two abilities would leave SA3 missing, and the numbers never
        // have a gap (FR-13).
        AbilityPrompt refused = await Fighters.PrepareAbilityAsync(fighter.Name, 4);

        Assert.Equal(FighterWriteOutcome.AbilityNumberOutOfRange, refused.Outcome);
        Assert.Equal(fighter.Name, refused.FighterName);
        Assert.Equal(2, refused.AbilityCount);
    }

    [Fact]
    public async Task A_new_ability_is_added_at_the_end_and_recorded_as_an_insert()
    {
        Variant fighter = await AddFighterAsync();

        AbilitySetResult set = await Fighters.SetAbilityAsync(
            fighter.Id,
            3,
            "  A third ability.  ",
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, set.Outcome);
        Assert.True(set.WasAdded);
        Assert.Equal(3, set.Ordinal);

        await using StanleyDbContext db = postgres.CreateContext();
        List<SignatureAbility> block = await BlockAsync(db, fighter.Id);

        Assert.Equal([1, 2, 3], block.Select(ability => (int)ability.Ordinal));
        Assert.Equal("A third ability.", block[2].AbilityText);

        ChangeRecord record = await SingleRecordAsync(
            db,
            ChangeEntityType.SignatureAbility,
            Key(fighter.Id, 3));

        Assert.Equal(ChangeAction.Insert, record.Action);
        Assert.Equal("A third ability.", (string?)JsonNode.Parse(record.After!)!["abilityText"]);
    }

    [Fact]
    public async Task An_existing_ability_is_rewritten_and_recorded_as_an_update()
    {
        Variant fighter = await AddFighterAsync();

        AbilitySetResult set = await Fighters.SetAbilityAsync(
            fighter.Id,
            1,
            "Rewritten.",
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, set.Outcome);
        Assert.False(set.WasAdded);

        await using StanleyDbContext db = postgres.CreateContext();
        List<SignatureAbility> block = await BlockAsync(db, fighter.Id);

        // Rewritten in place: an edit is not an addition, and the block still has two.
        Assert.Equal(2, block.Count);
        Assert.Equal("Rewritten.", block[0].AbilityText);

        ChangeRecord record = await SingleRecordAsync(
            db,
            ChangeEntityType.SignatureAbility,
            Key(fighter.Id, 1));

        Assert.Equal(ChangeAction.Update, record.Action);
        Assert.Equal("Test ability 1.", (string?)JsonNode.Parse(record.Before!)!["abilityText"]);
        Assert.Equal("Rewritten.", (string?)JsonNode.Parse(record.After!)!["abilityText"]);
    }

    [Fact]
    public async Task Blank_ability_text_and_text_past_the_embed_limit_are_refused()
    {
        Variant fighter = await AddFighterAsync();

        AbilitySetResult blank = await Fighters.SetAbilityAsync(fighter.Id, 1, "   ", Editor);
        Assert.Equal(FighterWriteOutcome.ValueMissing, blank.Outcome);

        AbilitySetResult tooLong = await Fighters.SetAbilityAsync(
            fighter.Id,
            1,
            new string('x', SignatureAbility.MaxTextLength + 1),
            Editor);

        // FR-13: 1,024 characters, the most a Discord embed field shows. The column has the
        // same check, which would fail the command rather than refuse it politely.
        Assert.Equal(FighterWriteOutcome.AbilityTextTooLong, tooLong.Outcome);

        await using StanleyDbContext db = postgres.CreateContext();
        List<SignatureAbility> block = await BlockAsync(db, fighter.Id);
        Assert.Equal("Test ability 1.", block[0].AbilityText);
    }

    [Fact]
    public async Task Removing_abilities_takes_the_whole_block_and_records_every_row()
    {
        Variant fighter = await AddFighterAsync();

        AbilityRemovalResult removed = await Fighters.RemoveAbilitiesAsync(fighter.Id, Editor);

        Assert.Equal(FighterWriteOutcome.Ok, removed.Outcome);
        Assert.Equal(2, removed.RemovedCount);

        await using StanleyDbContext db = postgres.CreateContext();
        Assert.Empty(await BlockAsync(db, fighter.Id));

        // The fighter stays, and so does its ability name: a fighter with no abilities is valid
        // while the block is being re-entered (database design section 3.3).
        Variant stored = await db.Variants.SingleAsync(variant => variant.Id == fighter.Id);
        Assert.Equal("TEST ABILITY", stored.SaName);

        // One record per row, because each row is what an undo would put back (FR-16).
        foreach (short ordinal in new short[] { 1, 2 })
        {
            ChangeRecord record = await SingleRecordAsync(
                db,
                ChangeEntityType.SignatureAbility,
                Key(fighter.Id, ordinal));

            Assert.Equal(ChangeAction.Delete, record.Action);
            Assert.Null(record.After);
        }

        // And the block starts again at SA1.
        AbilityPrompt prompt = await Fighters.PrepareAbilityAsync(fighter.Name, null);
        Assert.Equal(1, prompt.Ordinal);

        AbilityRemovalResult again = await Fighters.RemoveAbilitiesAsync(fighter.Id, Editor);
        Assert.Equal(FighterWriteOutcome.NoAbilities, again.Outcome);
    }

    [Fact]
    public async Task A_gap_in_the_sa_numbers_is_closed_by_the_next_write()
    {
        Variant fighter = await AddFighterAsync();

        // A gap the commands cannot make, written around them: SA1 gone and SA2 left behind.
        await using (StanleyDbContext seed = postgres.CreateContext())
        {
            await seed.SignatureAbilities
                .Where(ability => ability.VariantId == fighter.Id && ability.Ordinal == 1)
                .ExecuteDeleteAsync();
        }

        AbilitySetResult set = await Fighters.SetAbilityAsync(
            fighter.Id,
            2,
            "The only ability left.",
            Editor);

        Assert.Equal(FighterWriteOutcome.Ok, set.Outcome);

        await using StanleyDbContext db = postgres.CreateContext();
        List<SignatureAbility> block = await BlockAsync(db, fighter.Id);

        // Renumbered: the ordinals run from 1 with nothing missing (database design 3.3).
        Assert.Equal([1], block.Select(ability => (int)ability.Ordinal));
        Assert.Equal("The only ability left.", block[0].AbilityText);
    }

    [Fact]
    public async Task Removing_a_fighter_takes_its_abilities_and_aliases_and_records_them_all()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Doomed");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        int aliasId;

        await using (StanleyDbContext seed = postgres.CreateContext())
        {
            aliasId = await seed.VariantAliases
                .Where(row => row.VariantId == fighter.Id)
                .Select(row => row.Id)
                .SingleAsync();
        }

        FighterRemovalResult removed = await Fighters.RemoveAsync(fighter.Id, Editor);

        Assert.Equal(FighterWriteOutcome.Ok, removed.Outcome);
        Assert.Equal(fighter.Name, removed.FighterName);
        Assert.Equal(2, removed.RemovedAbilities);
        Assert.Equal(1, removed.RemovedAliases);

        await using StanleyDbContext db = postgres.CreateContext();

        Assert.False(await db.Variants.AnyAsync(variant => variant.Id == fighter.Id));
        Assert.Empty(await BlockAsync(db, fighter.Id));
        Assert.False(await db.VariantAliases.AnyAsync(row => row.VariantId == fighter.Id));

        // The fighter, both abilities and the alias are each recorded: a cascade leaves no
        // trace of what it took, and FR-15 is what a developer reads to put it back.
        ChangeRecord variantRecord = await SingleRecordAsync(
            db,
            ChangeEntityType.Variant,
            Key(fighter.Id));

        Assert.Equal(ChangeAction.Delete, variantRecord.Action);
        Assert.Equal(fighter.Name, (string?)JsonNode.Parse(variantRecord.Before!)!["name"]);

        ChangeRecord aliasRecord = await SingleRecordAsync(
            db,
            ChangeEntityType.VariantAlias,
            Key(aliasId),
            ChangeAction.Delete);

        Assert.Equal(alias, (string?)JsonNode.Parse(aliasRecord.Before!)!["alias"]);

        // Built outside the query: the key is text in the database, and the method that builds
        // it has no SQL translation.
        string firstAbility = Key(fighter.Id, 1);
        string secondAbility = Key(fighter.Id, 2);

        Assert.Equal(
            2,
            await db.ChangeRecords.CountAsync(record =>
                record.EntityType == ChangeEntityType.SignatureAbility
                && record.Action == ChangeAction.Delete
                && (record.EntityKey == firstAbility || record.EntityKey == secondAbility)));
    }

    [Fact]
    public async Task A_write_finds_its_fighter_by_an_alias_but_never_by_a_typo()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Known As");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        FighterTarget? byAlias = await Fighters.FindTargetAsync(alias);

        Assert.NotNull(byAlias);
        Assert.Equal(fighter.Id, byAlias.VariantId);
        Assert.Equal(2, byAlias.AbilityCount);
        Assert.Equal(1, byAlias.AliasCount);

        // Punctuation and spacing are absorbed by normalizing, so this still matches.
        Assert.NotNull(await Fighters.FindTargetAsync($"  {fighter.Name.ToUpperInvariant()}!  "));

        // A typo does not. The fuzzy step of FR-02 is for reading, not for deciding which
        // fighter an edit or a removal lands on.
        string typo = fighter.Name.Replace("Fighter", "Fightr", StringComparison.Ordinal);

        Assert.Null(await Fighters.FindTargetAsync(typo));
    }

    [Fact]
    public async Task Character_suggestions_offer_the_characters_that_exist()
    {
        Character character = await AddCharacterAsync();

        IReadOnlyList<string> byPrefix = await Fighters.SuggestCharactersAsync(character.Name);

        Assert.Contains(character.Name, byPrefix);

        // An empty box offers characters rather than nothing, and never more than Discord shows.
        IReadOnlyList<string> empty = await Fighters.SuggestCharactersAsync(string.Empty);

        Assert.NotEmpty(empty);
        Assert.True(empty.Count <= FighterLookup.MaxSuggestions);
    }

    /// <summary>A fighter of this test's own, with SA1 and SA2, saved and detached.</summary>
    private async Task<Variant> AddFighterAsync()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant fighter = TestFighters.NewVariant();
        db.Variants.Add(fighter);
        await db.SaveChangesAsync();

        return fighter;
    }

    /// <summary>
    /// A character of this test's own. The editor commands never create one: a fighter is a
    /// variant of a character that exists (FR-12).
    /// </summary>
    private async Task<Character> AddCharacterAsync()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        var character = new Character { Name = Unique("Test Character") };
        db.Characters.Add(character);
        await db.SaveChangesAsync();

        return character;
    }

    private static FighterDraft NewDraft(
        string name,
        string character,
        string rarity = Rarity,
        string element = Element) =>
        new(
            name,
            character,
            rarity,
            element,
            "TEST ABILITY",
            BaseAtk: 617,
            BaseHp: 4169,
            BaseFs: 919,
            PortraitUrl: "https://example.test/portrait.png");

    /// <summary>
    /// A suffix per name, because <c>variants.name</c>, <c>variants.slug</c>,
    /// <c>characters.name</c> and <c>alias_slug</c> are unique and the suite shares one database.
    /// </summary>
    private static string Unique(string name) => $"{name} {Guid.NewGuid():N}";

    private static Task<List<SignatureAbility>> BlockAsync(StanleyDbContext db, int variantId) =>
        db.SignatureAbilities
            .AsNoTracking()
            .Where(ability => ability.VariantId == variantId)
            .OrderBy(ability => ability.Ordinal)
            .ToListAsync();

    /// <summary>
    /// The one change record for an entity, or the one of that action when a row has more than
    /// one (its insert and then its delete).
    /// </summary>
    private static async Task<ChangeRecord> SingleRecordAsync(
        StanleyDbContext db,
        ChangeEntityType entityType,
        string entityKey,
        ChangeAction? action = null)
    {
        IQueryable<ChangeRecord> records = db.ChangeRecords
            .Where(record => record.EntityType == entityType && record.EntityKey == entityKey);

        if (action is ChangeAction wanted)
        {
            records = records.Where(record => record.Action == wanted);
        }

        return await records.SingleAsync();
    }

    /// <summary>
    /// A change record's <c>entity_key</c>, built as the repository builds it: text, and for a
    /// signature ability the pair that is its key.
    /// </summary>
    private static string Key(int id) => id.ToString(CultureInfo.InvariantCulture);

    private static string Key(int variantId, short ordinal) =>
        Key(variantId) + ":" + ordinal.ToString(CultureInfo.InvariantCulture);
}
