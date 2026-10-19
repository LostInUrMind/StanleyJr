using Microsoft.EntityFrameworkCore;
using Stanley.Data;
using Testcontainers.PostgreSql;

namespace Stanley.Data.Tests;

/// <summary>
/// A real PostgreSQL for the duration of the test run, with the migrations applied.
/// </summary>
/// <remarks>
/// <para>
/// The schema is what is being tested — CHECK constraints, cascades, the <c>pg_trgm</c>
/// extension and three views — and none of that exists in an in-memory provider. Docker must be
/// running; these tests are skipped by filtering on the <c>RequiresDocker</c> trait when it is
/// not (see the README).
/// </para>
/// <para>
/// The image is pinned so a test run cannot change behaviour because a tag moved.
/// </para>
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17.11-alpine")
        .WithDatabase("stanley")
        .WithUsername("stanley")
        .WithPassword("stanley")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // The migration under test, applied the same way a deploy applies it.
        await using StanleyDbContext db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>A context on the container's database. Each test gets its own.</summary>
    public StanleyDbContext CreateContext() => StanleyDbContextOptions.Create(ConnectionString);
}

/// <summary>
/// Shares one container across every database test: starting PostgreSQL is the slow part. The
/// tests therefore add rows with unique names and assert on deltas rather than on totals.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // xUnit's own name for this concept is a test collection.
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
#pragma warning restore CA1711
{
    public const string Name = "postgres";
}
