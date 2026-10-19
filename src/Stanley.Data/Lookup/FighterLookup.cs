using Microsoft.EntityFrameworkCore;
using Stanley.Core.Naming;
using Stanley.Core.Stats;
using Stanley.Data.Entities;

namespace Stanley.Data.Lookup;

/// <summary>
/// Finds fighters by what a user typed: the tolerant match behind <c>/fighter</c> (FR-02) and
/// the suggestions behind its autocomplete (FR-03).
/// </summary>
/// <remarks>
/// <para>
/// This is the only place that decides what a match is, so the command, the autocomplete and
/// any later command that takes a fighter name cannot drift apart. It lives in Stanley.Data
/// rather than Stanley.Core because the tolerant match is a query: the exact step is an index
/// lookup and the fallback is PostgreSQL's <c>similarity()</c>.
/// </para>
/// <para>
/// Both reads go through <c>searchable_names</c>, the view that unions variant names with
/// aliases (database design section 6.3), so neither has to repeat the union.
/// </para>
/// </remarks>
public sealed class FighterLookup(IDbContextFactory<StanleyDbContext> contextFactory)
{
    /// <summary>
    /// How alike two slugs must be for the fuzzy step of FR-02 to accept the match. The one
    /// cutoff in the system: raising it rejects more typos, lowering it invents matches.
    /// </summary>
    public const double SimilarityCutoff = 0.4;

    /// <summary>Discord shows at most this many autocomplete choices (FR-03).</summary>
    public const int MaxSuggestions = 25;

    /// <summary>The value the view's <c>kind</c> column carries for a variant's own name.</summary>
    /// <remarks>
    /// Internal rather than private because <c>/alias-admin add</c> reads the same view to find out
    /// whether an alias clashes with a fighter's own name or with another alias (FR-10), and
    /// the two must agree on what the column says.
    /// </remarks>
    internal const string NameKind = "name";

    public async Task<FighterCard?> FindCardAsync(
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

        if (await ResolveVariantIdAsync(db, slug, cancellationToken) is not int variantId)
        {
            return null;
        }

        Variant? variant = await db.Variants
            .AsNoTracking()
            .Include(v => v.Character)
            .Include(v => v.Tier)
            .Include(v => v.Element)
            .Include(v => v.SignatureAbilities)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        return variant is null ? null : ToCard(variant);
    }

    public async Task<WikiTarget?> FindWikiTargetAsync(
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

        if (await ResolveVariantIdAsync(db, slug, cancellationToken) is not int variantId)
        {
            return null;
        }

        return await db.Variants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => new WikiTarget(variant.Name, variant.WikiUrl))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FighterSuggestion>> SuggestAsync(
        string? input,
        CancellationToken cancellationToken = default)
    {
        await using StanleyDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        string slug = NameNormalizer.Normalize(input);

        // Joined rather than read from the view alone: an alias match has to suggest the
        // fighter it belongs to, which means reading that fighter's name and slug.
        var rows = db.SearchableNames
            .AsNoTracking()
            .Join(
                db.Variants,
                name => name.VariantId,
                variant => variant.Id,
                (name, variant) => new
                {
                    name.Label,
                    name.Slug,
                    name.Kind,
                    FighterName = variant.Name,
                    FighterSlug = variant.Slug,
                });

        var matches = slug.Length == 0
            ? await rows
                .OrderBy(row => row.FighterName)
                .ThenBy(row => row.Label)
                .Take(MaxSuggestions)
                .ToListAsync(cancellationToken)
            : await rows
                .Where(row =>
                    row.Slug.StartsWith(slug)
                    || EF.Functions.TrigramsSimilarity(row.Slug, slug) >= SimilarityCutoff)
                .OrderByDescending(row => row.Slug.StartsWith(slug))
                .ThenByDescending(row => EF.Functions.TrigramsSimilarity(row.Slug, slug))
                .ThenBy(row => row.Label)
                .Take(MaxSuggestions * 2)
                .ToListAsync(cancellationToken);

        var suggestions = new List<FighterSuggestion>(MaxSuggestions);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var match in matches)
        {
            if (!seen.Add(match.FighterSlug))
            {
                continue;
            }

            suggestions.Add(new FighterSuggestion(
                match.Label,
                match.FighterName,
                match.FighterSlug,
                !string.Equals(match.Kind, NameKind, StringComparison.Ordinal)));

            if (suggestions.Count == MaxSuggestions)
            {
                break;
            }
        }

        return suggestions;
    }

    internal static async Task<int?> ResolveVariantIdAsync(
        StanleyDbContext db,
        string slug,
        CancellationToken cancellationToken)
    {
        if (await ResolveVariantIdExactAsync(db, slug, cancellationToken) is int exact)
        {
            return exact;
        }

        // Step three. pg_trgm is enabled by the first migration; without it this throws rather
        // than quietly matching nothing.
        return await db.SearchableNames
            .AsNoTracking()
            .Where(name => EF.Functions.TrigramsSimilarity(name.Slug, slug) >= SimilarityCutoff)
            .OrderByDescending(name => EF.Functions.TrigramsSimilarity(name.Slug, slug))
            .ThenBy(name => name.Label)
            .Select(name => (int?)name.VariantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static Task<int?> ResolveVariantIdExactAsync(
        StanleyDbContext db,
        string slug,
        CancellationToken cancellationToken) =>
        db.SearchableNames
            .AsNoTracking()
            .Where(name => name.Slug == slug)
            .OrderBy(name => name.Kind == NameKind ? 0 : 1)
            .Select(name => (int?)name.VariantId)
            .FirstOrDefaultAsync(cancellationToken);

    private static FighterCard ToCard(Variant variant) => new(
        variant.Name,
        variant.Slug,
        variant.Tier.Name,
        variant.Element.Name,
        variant.Element.ColorHex,
        variant.Character.Name,
        variant.SaName,
        variant.BaseAtk,
        variant.BaseHp,
        variant.BaseFs,
        StatCalculator.Calculate(variant.BaseAtk, variant.BaseHp, variant.Tier.EvolutionsToMax),
        variant.ImageUrl,
        variant.IsRaidBoss,
        [..
            variant.SignatureAbilities
                .OrderBy(ability => ability.Ordinal)
                .Select(ability => new FighterAbility(ability.Ordinal, ability.AbilityText))
        ]);
}
