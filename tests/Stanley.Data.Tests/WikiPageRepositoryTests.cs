using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Stanley.Core.Naming;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;
using Stanley.Data.Wiki;

namespace Stanley.Data.Tests;

/// <summary>
/// What <c>/wiki</c> and <c>/wiki-admin</c> must do with the wiki pages that are not fighters:
/// resolve one however it is typed, add, correct and remove one under the rules the database
/// cannot state, and record every write.
/// </summary>
/// <remarks>
/// Against a real PostgreSQL, because the parts being tested are the unique index on
/// <c>key_slug</c>, the trigram match behind the suggestions and the <c>searchable_names</c>
/// view the fighter clash is read from. The tests share one database, so every key carries a
/// unique suffix and nothing asserts on totals.
/// </remarks>
[Collection(PostgresCollection.Name)]
[Trait("RequiresDocker", "true")]
public class WikiPageRepositoryTests(PostgresFixture postgres)
{
    /// <summary>The editor every write in these tests is attributed to.</summary>
    private static readonly EditorContext Editor = new(ActorUserId: 4242, GuildId: 99);

    private const string SomeUrl = "https://skullgirlsmobile.fandom.com/wiki/Prestige";

    private const string OtherUrl = "https://skullgirlsmobile.fandom.com/wiki/Marquee_Ability";

    private WikiPageRepository Pages => new(new TestContextFactory(postgres));

    [Fact]
    public async Task A_page_is_added_and_then_found_however_the_user_types_the_key()
    {
        string key = Unique("Prestige Mode");

        WikiPageWriteResult added = await Pages.AddAsync(key, SomeUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.Ok, added.Outcome);
        Assert.Equal(key, added.Key);
        Assert.Equal(SomeUrl, added.Url);

        // Matched on key_slug, so case, spaces and punctuation in the input do not have to
        // match the stored text, the same rule fighter names follow.
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key));
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key.ToUpperInvariant()));
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key.Replace(" ", string.Empty)));
    }

    [Fact]
    public async Task Two_keys_may_share_one_page()
    {
        // What replaced the synonyms of the hardcoded list: a page known by two names is two
        // rows with one URL, each removable on its own.
        string full = Unique("Critless Skill Tree");
        string shortKey = Unique("critless");

        Assert.Equal(
            WikiPageWriteOutcome.Ok,
            (await Pages.AddAsync(full, SomeUrl, Editor)).Outcome);
        Assert.Equal(
            WikiPageWriteOutcome.Ok,
            (await Pages.AddAsync(shortKey, SomeUrl, Editor)).Outcome);

        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(full));
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(shortKey));
    }

    [Fact]
    public async Task Nothing_is_found_for_a_key_that_was_never_added()
    {
        Assert.Null(await Pages.FindUrlAsync("zzqqxxnosuchwikipageanywhere"));

        // Punctuation alone normalizes to nothing, which must not match the first row it
        // happens to resemble.
        Assert.Null(await Pages.FindUrlAsync("???"));
        Assert.Null(await Pages.FindUrlAsync(null));
    }

    [Fact]
    public async Task A_key_that_is_already_a_page_is_refused_naming_that_page()
    {
        string key = Unique("Taken Twice");

        await Pages.AddAsync(key, SomeUrl, Editor);

        // Differently punctuated, so the refusal has to come from the slug rather than from
        // the text.
        WikiPageWriteResult refused =
            await Pages.AddAsync(key.ToUpperInvariant(), OtherUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.KeyTaken, refused.Outcome);
        Assert.Equal(key, refused.Detail);

        // And the page it clashed with is untouched.
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task A_key_that_is_a_fighters_name_is_refused_naming_the_fighter()
    {
        // /wiki resolves pages before fighters, so this key would hide the fighter.
        Variant fighter = await AddFighterAsync();

        WikiPageWriteResult refused = await Pages.AddAsync(fighter.Name, SomeUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.KeyFindsFighter, refused.Outcome);
        Assert.Equal(fighter.Name, refused.Detail);
        Assert.Null(await Pages.FindUrlAsync(fighter.Name));
    }

    [Fact]
    public async Task A_key_that_is_a_fighters_alias_is_refused_the_same_way()
    {
        Variant fighter = await AddFighterAsync();
        string alias = Unique("Hairball");

        await using (StanleyDbContext db = postgres.CreateContext())
        {
            db.VariantAliases.Add(TestFighters.NewAlias(
                await db.Variants.SingleAsync(v => v.Id == fighter.Id),
                alias));
            await db.SaveChangesAsync();
        }

        WikiPageWriteResult refused = await Pages.AddAsync(alias, SomeUrl, Editor);

        // Both halves of searchable_names are checked, because /wiki reads both.
        Assert.Equal(WikiPageWriteOutcome.KeyFindsFighter, refused.Outcome);
        Assert.Equal(fighter.Name, refused.Detail);
    }

    [Fact]
    public async Task A_key_with_no_letters_or_digits_is_refused()
    {
        WikiPageWriteResult refused = await Pages.AddAsync("???", SomeUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.KeyNotUsable, refused.Outcome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task A_blank_address_is_refused(string? url)
    {
        WikiPageWriteResult refused = await Pages.AddAsync(Unique("Blankish"), url, Editor);

        Assert.Equal(WikiPageWriteOutcome.UrlMissing, refused.Outcome);
        Assert.Equal(WikiPageField.Url, refused.Field);
    }

    [Theory]
    [InlineData("skullgirlsmobile.fandom.com/wiki/Prestige")]
    [InlineData("/wiki/Prestige")]
    [InlineData("javascript:alert(1)")]
    public async Task An_address_that_is_not_an_http_url_is_refused(string url)
    {
        WikiPageWriteResult refused = await Pages.AddAsync(Unique("Not A Url"), url, Editor);

        Assert.Equal(WikiPageWriteOutcome.UrlNotAUrl, refused.Outcome);
        Assert.Equal(url, refused.Detail);
    }

    [Fact]
    public async Task Adding_a_page_appends_one_insert_change_record_holding_the_new_row()
    {
        string key = Unique("Recorded");

        await Pages.AddAsync(key, SomeUrl, Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        WikiPage row = await RowAsync(db, key);
        ChangeRecord record = await SingleRecordAsync(db, row.Id);

        Assert.Equal(ChangeAction.Insert, record.Action);
        Assert.Equal(Editor.ActorUserId, record.ActorUserId);
        Assert.Equal(Editor.GuildId, record.GuildId);
        Assert.Null(record.Before);

        JsonNode after = JsonNode.Parse(record.After!)!;
        Assert.Equal(key, (string?)after["key"]);
        Assert.Equal(NameNormalizer.Normalize(key), (string?)after["keySlug"]);
        Assert.Equal(SomeUrl, (string?)after["url"]);
        Assert.Equal(row.Id, (int?)after["id"]);
    }

    [Fact]
    public async Task An_edit_changes_the_key_and_leaves_the_address_alone()
    {
        string key = Unique("Old Key");
        string renamed = Unique("New Key");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited = await Pages.EditAsync(key, renamed, null, Editor);

        Assert.Equal(WikiPageWriteOutcome.Ok, edited.Outcome);
        Assert.Equal(renamed, edited.Key);

        WikiPageFieldChange change = Assert.Single(edited.Changes);
        Assert.Equal(WikiPageField.Key, change.Field);
        Assert.Equal(key, change.Before);
        Assert.Equal(renamed, change.After);

        // The old key stops finding it, which is the point of the slug moving with the key.
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(renamed));
        Assert.Null(await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task An_edit_changes_the_address_and_leaves_the_key_alone()
    {
        string key = Unique("Repointed");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited = await Pages.EditAsync(key, null, OtherUrl, Editor);

        WikiPageFieldChange change = Assert.Single(edited.Changes);
        Assert.Equal(WikiPageField.Url, change.Field);
        Assert.Equal(SomeUrl, change.Before);
        Assert.Equal(OtherUrl, change.After);

        Assert.Equal(OtherUrl, await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task An_edit_can_change_both_at_once()
    {
        string key = Unique("Both Old");
        string renamed = Unique("Both New");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited = await Pages.EditAsync(key, renamed, OtherUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.Ok, edited.Outcome);
        Assert.Equal(2, edited.Changes.Count);
        Assert.Equal(OtherUrl, await Pages.FindUrlAsync(renamed));
    }

    [Fact]
    public async Task An_edit_that_gives_neither_value_changes_nothing()
    {
        string key = Unique("Untouched");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited = await Pages.EditAsync(key, null, null, Editor);

        Assert.Equal(WikiPageWriteOutcome.NothingGiven, edited.Outcome);
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task An_edit_that_gives_what_is_already_stored_changes_nothing()
    {
        string key = Unique("Same Again");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited = await Pages.EditAsync(key, key, SomeUrl, Editor);

        Assert.Equal(WikiPageWriteOutcome.NothingToChange, edited.Outcome);
        Assert.Equal(key, edited.Key);
    }

    [Fact]
    public async Task An_edit_may_recapitalize_a_key_without_clashing_with_itself()
    {
        // The slug does not move, so the page would otherwise be refused for holding its own
        // key.
        string key = Unique("mixed case");

        await Pages.AddAsync(key, SomeUrl, Editor);

        WikiPageWriteResult edited =
            await Pages.EditAsync(key, key.ToUpperInvariant(), null, Editor);

        Assert.Equal(WikiPageWriteOutcome.Ok, edited.Outcome);
        Assert.Equal(key.ToUpperInvariant(), edited.Key);
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task An_edit_onto_another_pages_key_is_refused()
    {
        string first = Unique("First Page");
        string second = Unique("Second Page");

        await Pages.AddAsync(first, SomeUrl, Editor);
        await Pages.AddAsync(second, OtherUrl, Editor);

        WikiPageWriteResult refused = await Pages.EditAsync(second, first, null, Editor);

        Assert.Equal(WikiPageWriteOutcome.KeyTaken, refused.Outcome);
        Assert.Equal(first, refused.Detail);

        // Both pages are as they were: the refusal happens inside the transaction.
        Assert.Equal(SomeUrl, await Pages.FindUrlAsync(first));
        Assert.Equal(OtherUrl, await Pages.FindUrlAsync(second));
    }

    [Fact]
    public async Task An_edit_of_a_page_that_does_not_exist_is_refused()
    {
        WikiPageWriteResult refused =
            await Pages.EditAsync("zzqqxxnosuchwikipageanywhere", "Anything", null, Editor);

        Assert.Equal(WikiPageWriteOutcome.PageNotFound, refused.Outcome);
    }

    [Fact]
    public async Task Editing_a_page_appends_one_update_change_record_holding_both_states()
    {
        string key = Unique("Audited");

        await Pages.AddAsync(key, SomeUrl, Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        int pageId = (await RowAsync(db, key)).Id;

        await Pages.EditAsync(key, null, OtherUrl, Editor);

        ChangeRecord record = await LatestRecordAsync(db, pageId);

        Assert.Equal(ChangeAction.Update, record.Action);

        JsonNode before = JsonNode.Parse(record.Before!)!;
        JsonNode after = JsonNode.Parse(record.After!)!;

        Assert.Equal(SomeUrl, (string?)before["url"]);
        Assert.Equal(OtherUrl, (string?)after["url"]);
        Assert.Equal(key, (string?)after["key"]);
    }

    [Fact]
    public async Task A_page_is_removed_and_stops_being_found()
    {
        string key = Unique("Shortlived");

        await Pages.AddAsync(key, SomeUrl, Editor);

        // Matched on key_slug, so the input need not be punctuated as the stored text is.
        RemovedWikiPage? removed =
            await Pages.RemoveAsync(key.Replace(" ", string.Empty), Editor);

        Assert.NotNull(removed);
        Assert.Equal(key, removed.Key);
        Assert.Equal(SomeUrl, removed.Url);
        Assert.Null(await Pages.FindUrlAsync(key));
    }

    [Fact]
    public async Task Removing_a_page_that_does_not_exist_removes_nothing()
    {
        Assert.Null(await Pages.RemoveAsync("zzqqxxnosuchwikipageanywhere", Editor));
        Assert.Null(await Pages.RemoveAsync("???", Editor));
    }

    [Fact]
    public async Task Removing_a_page_appends_one_delete_change_record_holding_the_old_row()
    {
        string key = Unique("Deleted");

        await Pages.AddAsync(key, SomeUrl, Editor);

        await using StanleyDbContext db = postgres.CreateContext();
        int pageId = (await RowAsync(db, key)).Id;

        await Pages.RemoveAsync(key, Editor);

        // Two records share the key now: the insert and the delete. The delete is the later
        // one, and it holds the only copy of the row that is left.
        ChangeRecord record = await LatestRecordAsync(db, pageId);

        Assert.Equal(ChangeAction.Delete, record.Action);
        Assert.Null(record.After);

        JsonNode before = JsonNode.Parse(record.Before!)!;
        Assert.Equal(key, (string?)before["key"]);
        Assert.Equal(SomeUrl, (string?)before["url"]);
        Assert.Equal(pageId, (int?)before["id"]);
    }

    [Fact]
    public async Task Suggestions_offer_the_pages_that_exist_with_their_address()
    {
        string key = Unique("Suggestible Page");

        await Pages.AddAsync(key, SomeUrl, Editor);

        string slug = NameNormalizer.Normalize(key);

        IReadOnlyList<WikiPageSuggestion> byPrefix = await Pages.SuggestAsync(slug[..12]);

        WikiPageSuggestion match = Assert.Single(byPrefix, s => s.KeySlug == slug);
        Assert.Equal(key, match.Key);
        Assert.Equal(SomeUrl, match.Url);

        // And a typo still finds it, which is the point of the trigram step.
        IReadOnlyList<WikiPageSuggestion> byTypo =
            await Pages.SuggestAsync(slug.Replace("sugg", "sugx", StringComparison.Ordinal));

        Assert.Contains(byTypo, s => s.KeySlug == slug);
    }

    [Fact]
    public async Task Suggestions_never_exceed_what_Discord_shows()
    {
        // An empty box offers the first keys alphabetically rather than nothing, and never
        // more than the 25 choices Discord shows.
        IReadOnlyList<WikiPageSuggestion> suggestions = await Pages.SuggestAsync(string.Empty);

        Assert.NotEmpty(suggestions);
        Assert.True(suggestions.Count <= FighterLookup.MaxSuggestions);
    }

    [Fact]
    public async Task Suggestions_honour_a_smaller_limit()
    {
        // /wiki shares its 25 choices with the fighters, so it asks for only a few pages;
        // without the limit being honoured, enough pages would hide every fighter.
        IReadOnlyList<WikiPageSuggestion> suggestions =
            await Pages.SuggestAsync(string.Empty, limit: 2);

        Assert.Equal(2, suggestions.Count);
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
    /// A suffix per key, because <c>key_slug</c> is unique and the suite shares one database.
    /// </summary>
    private static string Unique(string key) => $"{key} {Guid.NewGuid():N}";

    private static Task<WikiPage> RowAsync(StanleyDbContext db, string key)
    {
        string slug = NameNormalizer.Normalize(key);

        return db.WikiPages.AsNoTracking().SingleAsync(page => page.KeySlug == slug);
    }

    private static Task<ChangeRecord> SingleRecordAsync(StanleyDbContext db, int pageId)
    {
        string key = Key(pageId);

        return db.ChangeRecords.SingleAsync(record =>
            record.EntityType == ChangeEntityType.WikiPage && record.EntityKey == key);
    }

    private static Task<ChangeRecord> LatestRecordAsync(StanleyDbContext db, int pageId)
    {
        string key = Key(pageId);

        return db.ChangeRecords
            .Where(record =>
                record.EntityType == ChangeEntityType.WikiPage && record.EntityKey == key)
            .OrderByDescending(record => record.Id)
            .FirstAsync();
    }

    /// <summary>
    /// Built outside the query: the key is text in the database, and
    /// <c>int.ToString(IFormatProvider)</c> has no SQL translation.
    /// </summary>
    private static string Key(int pageId) => pageId.ToString(CultureInfo.InvariantCulture);
}
