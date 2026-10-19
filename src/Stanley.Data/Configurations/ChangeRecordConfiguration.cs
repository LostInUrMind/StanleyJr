using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Conversions;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class ChangeRecordConfiguration : IEntityTypeConfiguration<ChangeRecord>
{
    public void Configure(EntityTypeBuilder<ChangeRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Action).HasConversion(ChangeEnumConverters.Action);
        builder.Property(r => r.EntityType).HasConversion(ChangeEnumConverters.EntityType);

        builder.Property(r => r.Before).HasColumnType("jsonb");
        builder.Property(r => r.After).HasColumnType("jsonb");
        builder.Property(r => r.OccurredAt).HasDefaultValueSql("now()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_change_records_action",
                $"action IN ({Quote(ChangeEnumConverters.ActionValues)})");
            t.HasCheckConstraint(
                "ck_change_records_entity_type",
                $"entity_type IN ({Quote(ChangeEnumConverters.EntityTypeValues)})");
        });

        builder
            .HasIndex(r => new { r.EntityType, r.EntityKey, r.OccurredAt })
            .HasDatabaseName("ix_changes_entity")
            .IsDescending(false, false, true);

        builder.HasIndex(r => r.OccurredAt).HasDatabaseName("ix_changes_time");
    }

    private static string Quote(IReadOnlyList<string> values) =>
        string.Join(",", values.Select(v => $"'{v}'"));
}
