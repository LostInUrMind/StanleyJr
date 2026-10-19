using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;
using Stanley.Bot.Hosting;
using Stanley.Bot.Presentation;
using Stanley.Data;
using Stanley.Data.Aliases;
using Stanley.Data.Fighters;
using Stanley.Data.Lookup;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Configuration comes from appsettings.json, then the environment (Discord__Token), then user
// secrets in development. Nothing secret is ever read from the repository (NFR-06).
builder.Configuration.AddInMemoryCollection(
    GuildIdListConfiguration.ExpandCommaSeparated(
        builder.Configuration,
        $"{DiscordOptions.SectionName}:{nameof(DiscordOptions.TestGuildIds)}"));

builder.Services
    .AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetSection(DiscordOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));

// Card emoji. Not validated: every one of them is optional, and a card falls back to text for
// any that is missing (FR-23).
builder.Services
    .AddOptions<PresentationOptions>()
    .Bind(builder.Configuration.GetSection(PresentationOptions.SectionName));

builder.Logging.AddSimpleConsole(console =>
{
    console.SingleLine = true;
    console.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    console.UseUtcTimestamp = true;
});

string connectionString =
    builder.Configuration.GetConnectionString("Stanley")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:Stanley (or the ConnectionStrings__Stanley environment variable).");

builder.Services.AddPooledDbContextFactory<StanleyDbContext>(
    options => StanleyDbContextOptions.Configure(options, connectionString));

// Discord.Net wiring. Stanley asks for no privileged intents (NFR-02): slash commands arrive
// as interactions, and the Guilds intent is not privileged.
builder.Services.AddSingleton(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
    AlwaysDownloadUsers = false,
    LogGatewayIntentWarnings = false,
    LogLevel = LogSeverity.Info,
});
builder.Services.AddSingleton<DiscordSocketClient>();

builder.Services.AddSingleton(new InteractionServiceConfig
{
    DefaultRunMode = RunMode.Async,
    UseCompiledLambda = true,
});
builder.Services.AddSingleton(services => new InteractionService(
    services.GetRequiredService<DiscordSocketClient>(),
    services.GetRequiredService<InteractionServiceConfig>()));

builder.Services.AddSingleton<InteractionHandler>();

builder.Services.AddSingleton<FighterLookup>();
builder.Services.AddSingleton<AliasRepository>();
builder.Services.AddSingleton<FighterAdminRepository>();
builder.Services.AddSingleton<FighterPresentation>();
builder.Services.AddSingleton<FighterCardBuilder>();

// Order matters: hosted services start in the order they are registered, so the schema is in
// place before Discord can send the first interaction.
builder.Services.AddHostedService<DatabaseMigrationService>();
builder.Services.AddHostedService<DiscordBotService>();

IHost host = builder.Build();

try
{
    _ = host.Services.GetRequiredService<IOptions<DiscordOptions>>().Value;

    await host.RunAsync();
}
catch (OptionsValidationException ex)
{
    foreach (string failure in ex.Failures)
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

return 0;
