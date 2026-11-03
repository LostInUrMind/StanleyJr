using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stanley.Data.Entities;

namespace Stanley.Data.Configurations;

internal sealed class WikiPageConfiguration : IEntityTypeConfiguration<WikiPage>
{
    public void Configure(EntityTypeBuilder<WikiPage> builder)
    {
        builder.HasKey(p => p.Id);

        // What makes a duplicate key impossible, and the index /wiki resolves through.
        builder.HasIndex(p => p.KeySlug).IsUnique();

        // So the suggestions can fall back to similarity, as the alias ones do.
        builder
            .HasIndex([nameof(WikiPage.Key)], "ix_wiki_pages_trgm")
            .HasDatabaseName("ix_wiki_pages_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
    }
}
