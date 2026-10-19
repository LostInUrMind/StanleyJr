using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Stanley.Core.Naming;
using Stanley.Data.Aliases;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;

namespace Stanley.Data.Tests;

/// <summary>
/// What <c>/alias</c> must do: list a fighter's other names (FR-09), add one under the rules
/// the database cannot state (FR-10), remove one (FR-11), and record every write (FR-15).
/// </summary>
/// <remarks>
/// Against a real PostgreSQL, because the parts being tested are the trigram match, the unique
/// index on <c>alias_slug</c> and the <c>searchable_names</c> view. The tests share one
/// database, so every fighter and alias carries a unique suffix and nothing asserts on totals.
/// </remarks>
[Collection(PostgresCollection.Name)]
[Trait("RequiresDocker", "true")]
public class AliasRepositoryTests(PostgresFixture postgres)
{
    /// <summary>The editor every write in these tests is attributed to (FR-15).</summary>
    private static readonly EditorContext Editor = new(ActorUserId: 4242, GuildId: 99);

    private AliasRepository Aliases => new(new TestContextFactory(postgres));

    [Fact]
    public async Task An_alias_is_added_and_then_listed_under_the_fighters_name()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Hairball");

        AliasAddResult added = await Aliases.AddAsync(fighter.Name, alias, Editor);

        Assert.Equal(AliasAddOutcome.Added, added.Outcome);
        Assert.Equal(alias, added.Alias);
        Assert.Equal(fighter.Name, added.FighterName);

        AliasListing? listing = await Aliases.ListAsync(fighter.Name);

        Assert.NotNull(listing);
        Assert.Equal(fighter.Name, listing.FighterName);
        Assert.Equal([alias], listing.Aliases);
    }

    [Fact]
    public async Task A_fighter_with_no_aliases_lists_none_rather_than_failing()
    {
        Variant fighter = await AddFighterAsync();

        AliasListing? listing = await Aliases.ListAsync(fighter.Name);

        Assert.NotNull(listing);
        Assert.Empty(listing.Aliases);
    }

    [Fact]
    public async Task Listed_aliases_are_alphabetical()
    {
        Variant fighter = await AddFighterAsync();
        string first = Unique("Aardvark");
        string second = Unique("Mongoose");
        string third = Unique("Zebra");

        // Added out of order, so the ordering can only come from the query.
        await Aliases.AddAsync(fighter.Name, third, Editor);
        await Aliases.AddAsync(fighter.Name, first, Editor);
        await Aliases.AddAsync(fighter.Name, second, Editor);

        AliasListing? listing = await Aliases.ListAsync(fighter.Name);

        Assert.NotNull(listing);
        Assert.Equal([first, second, third], listing.Aliases);
    }

    [Fact]
    public async Task A_fighter_is_found_by_an_alias_it_already_has()
    {
        Variant fighter = await AddFighterAsync();
        string known = Unique("Kitty");
        string second = Unique("Feline");

        await Aliases.AddAsync(fighter.Name, known, Editor);

        // The fighter option goes through the same tolerant match as /fighter, whose second
        // step is an alias (FR-02), so an editor can name the fighter by one of its aliases.
        AliasAddResult added = await Aliases.AddAsync(known, second, Editor);

        Assert.Equal(AliasAddOutcome.Added, added.Outcome);
        Assert.Equal(fighter.Name, added.FighterName);
    }

    [Fact]
    public async Task An_alias_already_in_use_is_refused_naming_the_fighter_that_uses_it()
    {
        Variant owner = await AddFighterAsync();
        Variant other = await AddFighterAsync();
        string alias = Unique("Doublebooked");

        await Aliases.AddAsync(owner.Name, alias, Editor);

        AliasAddResult refused = await Aliases.AddAsync(other.Name, alias, Editor);

        Assert.Equal(AliasAddOutcome.AliasTaken, refused.Outcome);
        Assert.Equal(owner.Name, refused.FighterName);

        // Refused means refused: the row still belongs to the fighter that had it.
        AliasListing? listing = await Aliases.ListAsync(other.Name);
        Assert.NotNull(listing);
        Assert.Empty(listing.Aliases);
    }

    [Fact]
    public async Task An_alias_that_is_a_fighters_own_name_is_refused_naming_that_fighter()
    {
        Variant fighter = await AddFighterAsync();
        Variant taken = await AddFighterAsync();

        // The database cannot see across the two tables, so this rule lives in the repository
        // (database design section 3.6).
        AliasAddResult refused = await Aliases.AddAsync(fighter.Name, taken.Name, Editor);

        Assert.Equal(AliasAddOutcome.FighterNameTaken, refused.Outcome);
        Assert.Equal(taken.Name, refused.FighterName);
    }

    [Fact]
    public async Task An_alias_of_punctuation_alone_is_refused()
    {
        Variant fighter = await AddFighterAsync();

        AliasAddResult refused = await Aliases.AddAsync(fighter.Name, "!!! ???", Editor);

        Assert.Equal(AliasAddOutcome.AliasNotUsable, refused.Outcome);
        Assert.Null(refused.FighterName);
    }

    [Fact]
    public async Task Adding_an_alias_to_a_fighter_that_does_not_exist_matches_no_fighter()
    {
        AliasAddResult refused = await Aliases.AddAsync(
            "zzqqxxnosuchfighteranywhere",
            Unique("Orphan"),
            Editor);

        Assert.Equal(AliasAddOutcome.FighterNotFound, refused.Outcome);
        Assert.Null(refused.FighterName);
    }

    [Fact]
    public async Task An_alias_is_stored_as_typed_normalized_and_attributed_to_its_editor()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Ms. Fortune’s Pal");

        await Aliases.AddAsync(fighter.Name, $"  {alias}  ", Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        VariantAlias stored = await db.VariantAliases
            .SingleAsync(row => row.VariantId == fighter.Id);

        // Trimmed but otherwise untouched, because /alias list displays this text; the slug is
        // the normalized form the lookup matches on.
        Assert.Equal(alias, stored.Alias);
        Assert.Equal(NameNormalizer.Normalize(alias), stored.AliasSlug);
        Assert.Equal(Editor.ActorUserId, stored.CreatedByUserId);
    }

    [Fact]
    public async Task Adding_an_alias_appends_one_insert_change_record()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Recorded");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        VariantAlias stored = await db.VariantAliases
            .SingleAsync(row => row.VariantId == fighter.Id);

        ChangeRecord record = await SingleRecordAsync(db, stored.Id);

        Assert.Equal(ChangeAction.Insert, record.Action);
        Assert.Equal(ChangeEntityType.VariantAlias, record.EntityType);
        Assert.Equal(Editor.ActorUserId, record.ActorUserId);
        Assert.Equal(Editor.GuildId, record.GuildId);

        // An insert has no "before": there was no row (FR-15).
        Assert.Null(record.Before);

        JsonNode after = JsonNode.Parse(record.After!)!;
        Assert.Equal(alias, (string?)after["alias"]);
        Assert.Equal(fighter.Id, (int?)after["variantId"]);
    }

    [Fact]
    public async Task Removing_an_alias_reports_the_fighter_it_belonged_to()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Shortlived");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        RemovedAlias? removed = await Aliases.RemoveAsync(alias, Editor);

        Assert.NotNull(removed);
        Assert.Equal(alias, removed.Alias);
        Assert.Equal(fighter.Name, removed.FighterName);

        AliasListing? listing = await Aliases.ListAsync(fighter.Name);
        Assert.NotNull(listing);
        Assert.Empty(listing.Aliases);
    }

    [Fact]
    public async Task An_alias_is_removed_however_the_editor_punctuates_it()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Mr. Nick");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        // Matched on alias_slug, so the spacing and punctuation of the input do not have to
        // match the stored text (FR-02).
        RemovedAlias? removed = await Aliases.RemoveAsync(alias.Replace(".", string.Empty), Editor);

        Assert.NotNull(removed);
        Assert.Equal(alias, removed.Alias);
    }

    [Fact]
    public async Task Removing_an_alias_appends_one_delete_change_record_holding_the_old_row()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Deleted");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        int aliasId = await db.VariantAliases
            .Where(row => row.VariantId == fighter.Id)
            .Select(row => row.Id)
            .SingleAsync();

        await Aliases.RemoveAsync(alias, Editor);

        // Two records now share the key: the insert and the delete. The delete is the later
        // one, and it is the only copy of the row that is left (FR-15).
        string key = aliasId.ToString(CultureInfo.InvariantCulture);

        ChangeRecord record = await db.ChangeRecords
            .Where(r => r.EntityType == ChangeEntityType.VariantAlias && r.EntityKey == key)
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        Assert.Equal(ChangeAction.Delete, record.Action);
        Assert.Null(record.After);

        JsonNode before = JsonNode.Parse(record.Before!)!;
        Assert.Equal(alias, (string?)before["alias"]);
        Assert.Equal(aliasId, (int?)before["id"]);
    }

    [Fact]
    public async Task Removing_an_alias_that_does_not_exist_removes_nothing()
    {
        Assert.Null(await Aliases.RemoveAsync("zzqqxxnosuchaliasanywhere", Editor));

        // Punctuation alone normalizes to nothing, which must not match the first row it
        // happens to be similar to.
        Assert.Null(await Aliases.RemoveAsync("???", Editor));
    }

    [Fact]
    public async Task Suggestions_offer_the_aliases_that_exist_with_their_fighter()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Suggestible");

        await Aliases.AddAsync(fighter.Name, alias, Editor);

        string slug = NameNormalizer.Normalize(alias);

        IReadOnlyList<AliasSuggestion> byPrefix = await Aliases.SuggestAsync(slug[..12]);

        AliasSuggestion match = Assert.Single(byPrefix, s => s.AliasSlug == slug);
        Assert.Equal(alias, match.Alias);
        Assert.Equal(fighter.Name, match.FighterName);

        // And a typo still finds it, which is the point of the trigram step.
        IReadOnlyList<AliasSuggestion> byTypo =
            await Aliases.SuggestAsync(slug.Replace("sugg", "sugx", StringComparison.Ordinal));

        Assert.Contains(byTypo, s => s.AliasSlug == slug);
    }

    [Fact]
    public async Task Suggestions_never_exceed_what_Discord_shows()
    {
        // An empty box offers the first aliases alphabetically rather than nothing, and never
        // more than the 25 choices Discord shows (FR-03).
        IReadOnlyList<AliasSuggestion> suggestions = await Aliases.SuggestAsync(string.Empty);

        Assert.True(suggestions.Count <= FighterLookup.MaxSuggestions);
    }

    /// <summary>A fighter of this test's own, saved and detached.</summary>
    private async Task<Variant> AddFighterAsync()
    {
        await using StanleyDbContext db = postgres.CreateContext();

        Variant fighter = TestFighters.NewVariant();
        db.Variants.Add(fighter);
        await db.SaveChangesAsync();

        return fighter;
    }

    /// <summary>
    /// A suffix per alias, because <c>alias_slug</c> is unique and the suite shares one
    /// database.
    /// </summary>
    private static string Unique(string alias) => $"{alias} {Guid.NewGuid():N}";

    private static async Task<ChangeRecord> SingleRecordAsync(StanleyDbContext db, int aliasId)
    {
        // Built here rather than inside the query: the key is text in the database, and
        // int.ToString(IFormatProvider) has no SQL translation.
        string key = aliasId.ToString(CultureInfo.InvariantCulture);

        return await db.ChangeRecords.SingleAsync(record =>
            record.EntityType == ChangeEntityType.VariantAlias && record.EntityKey == key);
    }
}
