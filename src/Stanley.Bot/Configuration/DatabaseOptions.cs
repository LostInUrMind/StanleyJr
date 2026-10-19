namespace Stanley.Bot.Configuration;

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Whether the bot applies pending EF Core migrations when it starts.
    /// </summary>
    /// <remarks>
    /// On by default, any deployment that would rather apply migrations as
    /// its own step before the container starts sets this to <c>false</c>.
    /// </remarks>
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
