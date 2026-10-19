using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class VariantAliasConfiguration : IEntityTypeConfiguration<VariantAlias>
{
    public void Configure(EntityTypeBuilder<VariantAlias> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.AliasSlug).IsUnique();

        builder.HasIndex(a => a.VariantId).HasDatabaseName("ix_aliases_variant");

        // So FR-03 can suggest across names and aliases together.
        builder
            .HasIndex([nameof(VariantAlias.Alias)], "ix_aliases_trgm")
            .HasDatabaseName("ix_aliases_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        // Cascade: "the fighter's aliases are removed with it" (FR-14).
        builder
            .HasOne(a => a.Variant)
            .WithMany(v => v.Aliases)
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
