using Microsoft.EntityFrameworkCore;
using Stanley.Data.Entities;
using Stanley.Data.Views;

namespace Stanley.Data;

/// <summary>
/// The Stanley database (database design sections 3 and 6).
/// </summary>
/// <remarks>
/// Tables and columns are snake_case. That is set once, by
/// <c>UseSnakeCaseNamingConvention()</c> where the context is registered, instead of being
/// annotated on every property. <see cref="StanleyDbContextOptions"/> applies it, and both the
/// bot and the design-time factory go through it.
/// </remarks>
public class StanleyDbContext(DbContextOptions<StanleyDbContext> options) : DbContext(options)
{
    public DbSet<Character> Characters => Set<Character>();

    public DbSet<Variant> Variants => Set<Variant>();

    public DbSet<SignatureAbility> SignatureAbilities => Set<SignatureAbility>();

    public DbSet<Tier> Tiers => Set<Tier>();

    public DbSet<Element> Elements => Set<Element>();

    public DbSet<VariantAlias> VariantAliases => Set<VariantAlias>();

    public DbSet<ChangeRecord> ChangeRecords => Set<ChangeRecord>();

    /// <summary>Per-tier counts, excluding Raid Bosses. Read-only.</summary>
    public DbSet<TierVariantCount> TierVariantCounts => Set<TierVariantCount>();

    /// <summary>Per-element counts, excluding Raid Bosses. Read-only.</summary>
    public DbSet<ElementVariantCount> ElementVariantCounts => Set<ElementVariantCount>();

    /// <summary>Variant names and aliases in one place, for lookup and autocomplete. Read-only.</summary>
    public DbSet<SearchableName> SearchableNames => Set<SearchableName>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The similarity fallback in FR-02 needs it; the migration creates it.
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StanleyDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Timestamp();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        Timestamp();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Maintains <c>created_at</c> and <c>updated_at</c> for every timestamped entity, so no
    /// command has to remember to (database design sections 3.1 and 3.2). The columns also
    /// default to <c>now()</c> in the database, which covers rows inserted by plain SQL.
    /// </summary>
    private void Timestamp()
    {
        DateTime now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
                default:
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<SignatureAbility>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
