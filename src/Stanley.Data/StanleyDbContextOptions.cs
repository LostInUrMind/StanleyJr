using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Stanley.Data;

/// <summary>
/// The one place that says how to talk to the Stanley database. The bot, the importer, the
/// design-time factory and the database tests all call this, so none of them can configure the
/// provider differently.
/// </summary>
public static class StanleyDbContextOptions
{
    /// <summary>Applies the Npgsql provider, snake_case naming and the migrations assembly.</summary>
    public static DbContextOptionsBuilder Configure(
        DbContextOptionsBuilder builder,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(StanleyDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();
    }

    /// <summary>Builds a standalone context, for the importer and for tests.</summary>
    public static StanleyDbContext Create(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<StanleyDbContext>();
        Configure(builder, connectionString);
        return new StanleyDbContext(builder.Options);
    }
}
