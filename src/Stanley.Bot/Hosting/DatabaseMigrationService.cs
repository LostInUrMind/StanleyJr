using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Data;

namespace Stanley.Bot.Hosting;

/// <summary>
/// Applies pending EF Core migrations before the bot connects to Discord.
/// </summary>
/// <remarks>
/// Registered before <see cref="DiscordBotService"/>, and hosted services start in
/// registration order, so the schema is in place before any command can run. Switch it off
/// with <c>Database:ApplyMigrationsOnStartup</c> if a deployment applies migrations itself.
/// </remarks>
internal sealed class DatabaseMigrationService(
    IDbContextFactory<StanleyDbContext> contextFactory,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation(
                "Skipping migrations: Database:ApplyMigrationsOnStartup is false.");
            return;
        }

        await using StanleyDbContext context =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        DatabaseFacade database = context.Database;

        IEnumerable<string> pending = await database.GetPendingMigrationsAsync(cancellationToken);
        string[] names = [.. pending];

        if (names.Length == 0)
        {
            logger.LogInformation("Database schema is up to date.");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}.", names.Length, names);
        await database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
