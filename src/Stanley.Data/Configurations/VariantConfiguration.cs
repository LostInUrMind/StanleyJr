using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasIndex(v => v.Name).IsUnique();
        builder.HasIndex(v => v.Slug).IsUnique();

        // The similarity fallback in FR-02 and the suggestions in FR-03 read these. A second,
        // named index on the same column is how a trigram index sits beside the unique one.
        builder
            .HasIndex([nameof(Variant.Name)], "ix_variants_name_trgm")
            .HasDatabaseName("ix_variants_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(v => v.CharacterId).HasDatabaseName("ix_variants_character");
        builder.HasIndex(v => v.TierId).HasDatabaseName("ix_variants_tier");
        builder.HasIndex(v => v.ElementId).HasDatabaseName("ix_variants_element");

        builder.Property(v => v.IsRaidBoss).HasDefaultValue(false);
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("now()");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_variants_base_stats",
            "base_atk > 0 AND base_hp > 0 AND base_fs > 0"));

        // Restrict: reference rows are never deleted, and a character with variants cannot be
        // deleted until its variants are gone.
        builder
            .HasOne(v => v.Character)
            .WithMany(c => c.Variants)
            .HasForeignKey(v => v.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(v => v.Tier)
            .WithMany(t => t.Variants)
            .HasForeignKey(v => v.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(v => v.Element)
            .WithMany(e => e.Variants)
            .HasForeignKey(v => v.ElementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
