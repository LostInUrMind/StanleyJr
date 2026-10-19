using Microsoft.EntityFrameworkCore;

namespace Stanley.Data.Tests;

/// <summary>
/// Hands the repositories contexts on the test container's database.
/// </summary>
/// <remarks>
/// The bot registers a pooled factory; a repository only ever asks for a context and returns
/// it, so for a test one that opens a fresh context per call is the same contract without the
/// pool.
/// </remarks>
internal sealed class TestContextFactory(PostgresFixture postgres)
    : IDbContextFactory<StanleyDbContext>
{
    public StanleyDbContext CreateDbContext() => postgres.CreateContext();
}
