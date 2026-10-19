using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Views;

namespace Stanley.Data.Configurations;

/// <summary>
/// The three views are created by the migration (<see cref="Migrations"/>) and mapped keyless
/// here, so they can be queried but never written.
/// </summary>
internal sealed class TierVariantCountConfiguration : IEntityTypeConfiguration<TierVariantCount>
{
    public void Configure(EntityTypeBuilder<TierVariantCount> builder)
    {
        builder.HasNoKey().ToView(DatabaseViews.TierVariantCounts);
    }
}

internal sealed class ElementVariantCountConfiguration
    : IEntityTypeConfiguration<ElementVariantCount>
{
    public void Configure(EntityTypeBuilder<ElementVariantCount> builder)
    {
        builder.HasNoKey().ToView(DatabaseViews.ElementVariantCounts);
    }
}

internal sealed class SearchableNameConfiguration : IEntityTypeConfiguration<SearchableName>
{
    public void Configure(EntityTypeBuilder<SearchableName> builder)
    {
        builder.HasNoKey().ToView(DatabaseViews.SearchableNames);
    }
}
