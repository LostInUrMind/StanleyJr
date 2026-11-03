using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stanley.Core.Naming;
using Stanley.Core.Validation;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;

namespace Stanley.Data.Wiki;

/// <summary>
/// The wiki pages that are not fighters: resolving one for <c>/wiki</c>, suggesting them, and
/// the editor writes behind <c>/wiki-admin</c>.
/// </summary>
/// <remarks>
/// <para>
/// These pages were a hardcoded list in <c>Stanley.Core</c> until they became a table, so that
/// adding one is an editor's command rather than a developer's deploy — the same move
/// <c>/fighter-admin</c> and <c>/alias-admin</c> made for fighters and aliases.
/// </para>
/// <para>
/// A key is matched on its slug alone, exactly, and never by similarity: the keys are
/// deliberate words an editor chose, and <c>/wiki</c> resolves them before it looks for a
/// fighter, so a loose match here would answer with a page where a fighter was asked for. The
/// similarity step is in <see cref="SuggestAsync"/>, where a typo costs nothing.
/// </para>
/// </remarks>
public sealed class WikiPageRepository(IDbContextFactory<StanleyDbContext> contextFactory)
{
    /// <summary>
    /// The URL of the wiki page <paramref name="input"/> names, or null when it names none.
    /// The first step of <c>/wiki</c>.
    /// </summary>
    public async Task<string?> FindUrlAsync(
        string? input,
        CancellationToken cancellationToken = default)
    {
        string slug = NameNormalizer.Normalize(input);

        if (slug.Length == 0)
        {
            return null;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.WikiPages
            .AsNoTracking()
            .Where(page => page.KeySlug == slug)
            .Select(page => page.Url)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The wiki pages worth offering for what has been typed so far. An empty box offers the
    /// first keys alphabetically rather than nothing, as every other suggestion does.
    /// </summary>
    /// <param name="typed">What the user has typed so far.</param>
    /// <param name="limit">
    /// How many to return at most. <c>/wiki</c> asks for a few, because its 25 choices are
    /// shared with the fighters and there is no limit on how many pages editors may add;
    /// <c>/wiki-admin</c> leaves it at everything Discord shows.
    /// </param>
    /// <param name="cancellationToken">Cancels the query.</param>
    public async Task<IReadOnlyList<WikiPageSuggestion>> SuggestAsync(
        string? typed,
        int limit = FighterLookup.MaxSuggestions,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        string slug = NameNormalizer.Normalize(typed);

        IQueryable<WikiPage> pages = db.WikiPages.AsNoTracking();

        IOrderedQueryable<WikiPage> matches = slug.Length == 0
            ? pages.OrderBy(page => page.Key)
            : pages
                .Where(page =>
                    page.KeySlug.StartsWith(slug)
                    || EF.Functions.TrigramsSimilarity(page.KeySlug, slug)
                        >= FighterLookup.SimilarityCutoff)
                // The same ranking as the fighter and alias suggestions: what the user is
                // typing the start of first, then whatever is closest.
                .OrderByDescending(page => page.KeySlug.StartsWith(slug))
                .ThenByDescending(page =>
                    EF.Functions.TrigramsSimilarity(page.KeySlug, slug))
                .ThenBy(page => page.Key);

        return await matches
            .Take(Math.Clamp(limit, 0, FighterLookup.MaxSuggestions))
            .Select(page => new WikiPageSuggestion(page.Key, page.KeySlug, page.Url))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a wiki page under <paramref name="key"/>, or states the rule that refused it.
    /// </summary>
    public async Task<WikiPageWriteResult> AddAsync(
        string? key,
        string? url,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string keyText = key?.Trim() ?? string.Empty;
        string keySlug = NameNormalizer.Normalize(keyText);
        string urlText = url?.Trim() ?? string.Empty;

        if (keySlug.Length == 0)
        {
            return WikiPageWriteResult.KeyNotUsable(keyText);
        }

        if (CheckUrl(urlText) is WikiPageWriteResult refused)
        {
            return refused;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        if (await ClashAsync(db, keySlug, keyText, cancellationToken) is WikiPageWriteResult clash)
        {
            return clash;
        }

        var row = new WikiPage
        {
            Key = keyText,
            KeySlug = keySlug,
            Url = urlText,
            CreatedByUserId = editor.ActorUserId,
        };

        db.WikiPages.Add(row);

        // Saved before the change record because the record's entity_key is the row's id,
        // which the database assigns.
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeRecords.Add(ChangeRecords.Insert(
            editor,
            ChangeEntityType.WikiPage,
            EntityKey(row.Id),
            WikiPageSnapshot.Of(row)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return WikiPageWriteResult.Added(row.Key, row.Url);
    }

    /// <summary>
    /// Changes the key of the page <paramref name="page"/> names, or its URL, or both.
    /// </summary>
    /// <remarks>
    /// Whichever option the editor left out is left alone, and a value that is already stored
    /// counts as nothing to change rather than as a change of no effect.
    /// </remarks>
    public async Task<WikiPageWriteResult> EditAsync(
        string? page,
        string? key,
        string? url,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string pageSlug = NameNormalizer.Normalize(page);

        if (pageSlug.Length == 0)
        {
            return WikiPageWriteResult.NotFound();
        }

        if (key is null && url is null)
        {
            return WikiPageWriteResult.NothingGiven();
        }

        string? keyText = key?.Trim();
        string? keySlug = keyText is null ? null : NameNormalizer.Normalize(keyText);

        if (keySlug is not null && keySlug.Length == 0)
        {
            return WikiPageWriteResult.KeyNotUsable(keyText!);
        }

        string? urlText = url?.Trim();

        if (urlText is not null && CheckUrl(urlText) is WikiPageWriteResult refused)
        {
            return refused;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        WikiPage? row = await db.WikiPages
            .FirstOrDefaultAsync(entity => entity.KeySlug == pageSlug, cancellationToken);

        if (row is null)
        {
            return WikiPageWriteResult.NotFound();
        }

        // Taken before anything is applied: this is the record's before, and what an undo
        // would restore.
        WikiPageSnapshot before = WikiPageSnapshot.Of(row);
        var changes = new List<WikiPageFieldChange>(2);

        if (keyText is not null && !string.Equals(keyText, row.Key, StringComparison.Ordinal))
        {
            // Only when the slug moves: recapitalizing a key changes what is displayed and
            // nothing about what it matches, so it cannot clash with anything new.
            if (!string.Equals(keySlug, row.KeySlug, StringComparison.Ordinal)
                && await ClashAsync(db, keySlug!, keyText, cancellationToken)
                    is WikiPageWriteResult clash)
            {
                return clash;
            }

            changes.Add(new WikiPageFieldChange(WikiPageField.Key, row.Key, keyText));
            row.Key = keyText;
            row.KeySlug = keySlug!;
        }

        if (urlText is not null && !string.Equals(urlText, row.Url, StringComparison.Ordinal))
        {
            changes.Add(new WikiPageFieldChange(WikiPageField.Url, row.Url, urlText));
            row.Url = urlText;
        }

        if (changes.Count == 0)
        {
            return WikiPageWriteResult.NothingToChange(row.Key);
        }

        // Saved before the record so that updated_at, which SaveChanges sets, is in the after
        // snapshot rather than a value one save behind.
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeRecords.Add(ChangeRecords.Update(
            editor,
            ChangeEntityType.WikiPage,
            EntityKey(row.Id),
            before,
            WikiPageSnapshot.Of(row)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return WikiPageWriteResult.Edited(row.Key, row.Url, changes);
    }

    /// <summary>
    /// Removes the wiki page <paramref name="page"/> names, or returns null when no page
    /// matches.
    /// </summary>
    public async Task<RemovedWikiPage?> RemoveAsync(
        string? page,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string pageSlug = NameNormalizer.Normalize(page);

        if (pageSlug.Length == 0)
        {
            return null;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        WikiPage? row = await db.WikiPages
            .FirstOrDefaultAsync(entity => entity.KeySlug == pageSlug, cancellationToken);

        if (row is null)
        {
            return null;
        }

        var removed = new RemovedWikiPage(row.Key, row.Url);

        // Taken before the delete: afterwards the change record holds the only copy of the
        // row, and it is what an undo would restore.
        WikiPageSnapshot before = WikiPageSnapshot.Of(row);

        db.WikiPages.Remove(row);
        db.ChangeRecords.Add(ChangeRecords.Delete(
            editor,
            ChangeEntityType.WikiPage,
            EntityKey(row.Id),
            before));

        // No explicit transaction: the delete and its record are one SaveChanges, which EF
        // Core already sends as a single transaction.
        await db.SaveChangesAsync(cancellationToken);

        return removed;
    }

    /// <summary>
    /// The refusal for a key that something already answers to, or null when it is free.
    /// </summary>
    /// <remarks>
    /// Two pools are checked, because <c>/wiki</c> reads both. Another page's key is the unique
    /// index's business as well; checking it here is what turns the collision into a reply
    /// naming the page rather than a failed command. A fighter's name or alias is matched
    /// exactly, not by similarity: a key merely similar to a fighter's name shadows nothing,
    /// because <c>/wiki</c> resolves keys exactly.
    /// </remarks>
    private static async Task<WikiPageWriteResult?> ClashAsync(
        StanleyDbContext db,
        string keySlug,
        string keyText,
        CancellationToken cancellationToken)
    {
        string? owner = await db.WikiPages
            .AsNoTracking()
            .Where(page => page.KeySlug == keySlug)
            .Select(page => page.Key)
            .FirstOrDefaultAsync(cancellationToken);

        if (owner is not null)
        {
            return WikiPageWriteResult.KeyTaken(keyText, owner);
        }

        if (await FighterLookup.ResolveVariantIdExactAsync(db, keySlug, cancellationToken)
            is not int variantId)
        {
            return null;
        }

        string? fighterName = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => variant.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return fighterName is null
            ? null
            : WikiPageWriteResult.KeyFindsFighter(keyText, fighterName);
    }

    /// <summary>
    /// The refusal for a URL that is blank or is not a web address, or null when it is one.
    /// </summary>
    private static WikiPageWriteResult? CheckUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return WikiPageWriteResult.UrlMissing();
        }

        return WebAddress.IsAbsoluteHttpUrl(url) ? null : WikiPageWriteResult.NotAUrl(url);
    }

    /// <summary>
    /// A change record's <c>entity_key</c>: text, because for one entity the key is composite
    /// and the row may already be gone (database design section 3.7).
    /// </summary>
    private static string EntityKey(int pageId) =>
        pageId.ToString(CultureInfo.InvariantCulture);
}
