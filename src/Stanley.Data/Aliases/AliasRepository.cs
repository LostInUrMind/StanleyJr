using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stanley.Core.Naming;
using Stanley.Data.Changes;
using Stanley.Data.Entities;
using Stanley.Data.Lookup;

namespace Stanley.Data.Aliases;

/// <summary>
/// The alternative names players use for a fighter: listing them (FR-09), adding one (FR-10)
/// and removing one (FR-11).
/// </summary>
public sealed class AliasRepository(IDbContextFactory<StanleyDbContext> contextFactory)
{
    /// <summary>
    /// The aliases of the fighter <paramref name="fighter"/> names, or null when it names no
    /// fighter (FR-09).
    /// </summary>
    public async Task<AliasListing?> ListAsync(
        string? fighter,
        CancellationToken cancellationToken = default)
    {
        string slug = NameNormalizer.Normalize(fighter);

        if (slug.Length == 0)
        {
            return null;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await FighterLookup.ResolveVariantIdAsync(db, slug, cancellationToken) is not int id)
        {
            return null;
        }

        var found = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == id)
            .Select(variant => new
            {
                variant.Name,
                Aliases = variant.Aliases
                    .OrderBy(alias => alias.Alias)
                    .Select(alias => alias.Alias)
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return found is null ? null : new AliasListing(found.Name, found.Aliases);
    }

    /// <summary>
    /// Adds <paramref name="alias"/> as another name for the fighter <paramref name="fighter"/>
    /// names (FR-10).
    /// </summary>
    public async Task<AliasAddResult> AddAsync(
        string? fighter,
        string? alias,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string aliasText = alias?.Trim() ?? string.Empty;
        string aliasSlug = NameNormalizer.Normalize(aliasText);
        string fighterSlug = NameNormalizer.Normalize(fighter);

        if (fighterSlug.Length == 0)
        {
            return AliasAddResult.FighterNotFound(aliasText);
        }

        if (aliasSlug.Length == 0)
        {
            return AliasAddResult.AliasNotUsable(aliasText);
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        if (await FighterLookup.ResolveVariantIdAsync(db, fighterSlug, cancellationToken)
            is not int variantId)
        {
            return AliasAddResult.FighterNotFound(aliasText);
        }

        string? fighterName = await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => variant.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (fighterName is null)
        {
            return AliasAddResult.FighterNotFound(aliasText);
        }

        var clash = await db.SearchableNames
            .AsNoTracking()
            .Where(name => name.Slug == aliasSlug)
            .Join(
                db.Variants,
                name => name.VariantId,
                variant => variant.Id,
                (name, variant) => new { name.Kind, Owner = variant.Name })
            .OrderBy(row => row.Kind == FighterLookup.NameKind ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

        if (clash is not null)
        {
            return clash.Kind == FighterLookup.NameKind
                ? AliasAddResult.FighterNameTaken(aliasText, clash.Owner)
                : AliasAddResult.AliasTaken(aliasText, clash.Owner);
        }

        var row = new VariantAlias
        {
            VariantId = variantId,
            Alias = aliasText,
            AliasSlug = aliasSlug,
            CreatedByUserId = editor.ActorUserId,
        };

        db.VariantAliases.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeRecords.Add(ChangeRecords.Insert(
            editor,
            ChangeEntityType.VariantAlias,
            Key(row.Id),
            AliasSnapshot.Of(row)));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AliasAddResult.Added(row.Alias, fighterName);
    }

    /// <summary>
    /// Removes the alias <paramref name="alias"/> names, or returns null when no alias matches
    /// (FR-11).
    /// </summary>
    public async Task<RemovedAlias?> RemoveAsync(
        string? alias,
        EditorContext editor,
        CancellationToken cancellationToken = default)
    {
        string aliasSlug = NameNormalizer.Normalize(alias);

        if (aliasSlug.Length == 0)
        {
            return null;
        }

        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        VariantAlias? row = await db.VariantAliases
            .Include(entity => entity.Variant)
            .FirstOrDefaultAsync(entity => entity.AliasSlug == aliasSlug, cancellationToken);

        if (row is null)
        {
            return null;
        }

        var removed = new RemovedAlias(row.Alias, row.Variant.Name);

        // Taken before the delete: afterwards the change record holds the only copy of the row
        // (FR-15), and it is what an undo would restore (FR-16).
        AliasSnapshot before = AliasSnapshot.Of(row);

        db.VariantAliases.Remove(row);
        db.ChangeRecords.Add(ChangeRecords.Delete(
            editor,
            ChangeEntityType.VariantAlias,
            Key(row.Id),
            before));

        // No explicit transaction: the delete and its record are one SaveChanges, which EF
        // Core already sends as a single transaction.
        await db.SaveChangesAsync(cancellationToken);

        return removed;
    }

    /// <summary>
    /// Suggests existing aliases for what the editor has typed so far, for
    /// <c>/alias-admin remove</c>. An empty box offers the first aliases alphabetically rather than
    /// nothing.
    /// </summary>
    public async Task<IReadOnlyList<AliasSuggestion>> SuggestAsync(
        string? typed,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        string slug = NameNormalizer.Normalize(typed);

        IQueryable<VariantAlias> aliases = db.VariantAliases.AsNoTracking();

        IOrderedQueryable<VariantAlias> matches = slug.Length == 0
            ? aliases.OrderBy(alias => alias.Alias)
            : aliases
                .Where(alias =>
                    alias.AliasSlug.StartsWith(slug)
                    || EF.Functions.TrigramsSimilarity(alias.AliasSlug, slug)
                        >= FighterLookup.SimilarityCutoff)
                // The same ranking as the fighter suggestions: what the editor is typing the
                // start of first, then whatever is closest, so a typo still surfaces it.
                .OrderByDescending(alias => alias.AliasSlug.StartsWith(slug))
                .ThenByDescending(alias =>
                    EF.Functions.TrigramsSimilarity(alias.AliasSlug, slug))
                .ThenBy(alias => alias.Alias);

        return await matches
            .Take(FighterLookup.MaxSuggestions)
            .Select(alias => new AliasSuggestion(
                alias.Alias,
                alias.AliasSlug,
                alias.Variant.Name))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A change record's <c>entity_key</c>: text, because for one entity the key is composite
    /// and the row may already be gone (database design section 3.7).
    /// </summary>
    private static string Key(int aliasId) => aliasId.ToString(CultureInfo.InvariantCulture);
}
