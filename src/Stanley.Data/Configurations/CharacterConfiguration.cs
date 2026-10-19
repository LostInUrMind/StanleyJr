using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Name).IsUnique();

        // The snake_case convention puts no underscore between a digit and a letter, which
        // would give ma1name. 
        builder.Property(c => c.Ma1Name).HasColumnName("ma1_name");
        builder.Property(c => c.Ma1Des).HasColumnName("ma1_des");
        builder.Property(c => c.Ma2Name).HasColumnName("ma2_name");
        builder.Property(c => c.Ma2Des).HasColumnName("ma2_des");

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_characters_atk_ratio", "atk_ratio BETWEEN 1 AND 5");
            t.HasCheckConstraint("ck_characters_hp_ratio", "hp_ratio BETWEEN 1 AND 5");
        });
    }
}
