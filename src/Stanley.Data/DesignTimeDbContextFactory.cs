using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stanley.Data;

/// <summary>
/// Lets <c>dotnet ef</c> build a context without starting the bot, so migrations can be added
/// from this project alone.
/// </summary>
/// <remarks>
/// The connection string is only used when a command actually touches a database
/// (<c>database update</c>); adding a migration never connects. Override it with the
/// <c>STANLEY_DB_CONNECTION</c> environment variable.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StanleyDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=stanley;Username=stanley;Password=stanley";

    public StanleyDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("STANLEY_DB_CONNECTION")
            ?? LocalDevelopmentConnection;

        return StanleyDbContextOptions.Create(connectionString);
    }
}
