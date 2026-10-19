using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class TierConfiguration : IEntityTypeConfiguration<Tier>
{
    public void Configure(EntityTypeBuilder<Tier> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.Name).IsUnique();

        // Unique because the formula keys off it and two tiers needing the same number of
        // evolutions would mean one of them is wrong.
        builder.HasIndex(t => t.EvolutionsToMax).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_tiers_evolutions_to_max",
            "evolutions_to_max BETWEEN 0 AND 3"));

        builder.HasData(SeedData.Tiers);
    }
}
