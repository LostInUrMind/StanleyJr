using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class ElementConfiguration : IEntityTypeConfiguration<Element>
{
    public void Configure(EntityTypeBuilder<Element> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_elements_color_hex",
            "color_hex ~ '^#[0-9A-Fa-f]{6}$'"));

        builder.HasData(SeedData.Elements);
    }
}
