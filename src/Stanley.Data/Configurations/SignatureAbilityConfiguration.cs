using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class SignatureAbilityConfiguration : IEntityTypeConfiguration<SignatureAbility>
{
    public void Configure(EntityTypeBuilder<SignatureAbility> builder)
    {
        // The natural key is the pair, so one SA number cannot exist twice for a variant and
        // reading a card's abilities in order is one index scan. No surrogate id: nothing
        // refers to an ability, and `/fighter-admin ability fighter sa:<n>` addresses this key.
        builder.HasKey(a => new { a.VariantId, a.Ordinal });

        builder.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_signature_abilities_ordinal", "ordinal >= 1");
            t.HasCheckConstraint(
                "ck_signature_abilities_text_length",
                $"char_length(ability_text) <= {SignatureAbility.MaxTextLength}");
        });

        // Cascade: removing a variant removes its abilities (FR-14).
        builder
            .HasOne(a => a.Variant)
            .WithMany(v => v.SignatureAbilities)
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
