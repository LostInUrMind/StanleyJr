using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;

namespace Stanley.Bot.Hosting;

/// <summary>
/// Owns the Discord connection: logs in, registers the slash commands, and logs out cleanly on
/// shutdown.
/// </summary>
internal sealed class DiscordBotService(
    DiscordSocketClient client,
    InteractionService interactions,
    InteractionHandler handler,
    IOptions<DiscordOptions> options,
    ILogger<DiscordBotService> logger) : IHostedService
{
    private readonly DiscordOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Startup role check
        if (_options.HomeGuildId is null || _options.WikiEditorRoleId is null)
        {
            logger.LogWarning(
                "Discord:HomeGuildId or Discord:WikiEditorRoleId is not set, so the editor "
                + "commands will refuse every caller. A host-side run reads these from user "
                + "secrets or the environment; .env reaches the bot only inside Docker.");
        }

        client.Log += message => DiscordLogForwarder.ForwardAsync(logger, message);
        interactions.Log += message => DiscordLogForwarder.ForwardAsync(logger, message);
        client.Ready += OnReadyAsync;

        await handler.InitializeAsync();

        await client.LoginAsync(TokenType.Bot, _options.Token);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.StopAsync();
        await client.LogoutAsync();
    }

    /// <summary>
    /// Registers the commands once Discord says the connection is ready.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every scope Stanley uses is written exactly once, with everything that scope should
    /// hold: one bulk overwrite per test server, one for the home server, and one for the
    /// global set. A command renamed or removed in code therefore cannot survive a restart,
    /// which is what a stale <c>/alias add</c> was — offered by Discord long after the handler
    /// for it had gone, and reaching a bot with nothing to run.
    /// </para>
    /// <para>
    /// <c>RegisterCommandsGloballyAsync</c> and <c>RegisterCommandsToGuildAsync</c> are
    /// deliberately not used. They register every module that is not marked
    /// <c>[DontAutoRegister]</c> — the public half — and leave every scope they were not called
    /// for exactly as it was. Naming the modules per scope is what makes a scope's contents a
    /// decision rather than a leftover.
    /// </para>
    /// </remarks>
    private async Task OnReadyAsync()
    {
        try
        {
            // The editor commands are the modules kept out of automatic registration; the rest
            // are everyone's. AddModulesToGuildAsync and AddModulesGloballyAsync do include a
            // [DontAutoRegister] module when it is named explicitly, which is what lets the
            // attribute mark the split without hiding these commands from their own scope.
            ModuleInfo[] editorModules =
                [.. interactions.Modules.Where(module => module.DontAutoRegister)];

            ModuleInfo[] openModules =
                [.. interactions.Modules.Where(module => !module.DontAutoRegister)];

            // Worked out before anything is sent, so a server that is both a test server and
            // the home server is written once, with both halves, rather than twice.
            Dictionary<ulong, List<ModuleInfo>> guilds = [];

            foreach (ulong testGuildId in _options.TestGuildIds)
            {
                // Guild commands appear immediately, so development does not wait for Discord
                // to propagate a global change.
                guilds[testGuildId] = [.. openModules];
            }

            if (_options.HomeGuildId is ulong homeGuildId)
            {
                if (!guilds.TryGetValue(homeGuildId, out List<ModuleInfo>? modules))
                {
                    guilds[homeGuildId] = modules = [];
                }

                modules.AddRange(editorModules);
            }
            else if (editorModules.Length > 0)
            {
                logger.LogWarning(
                    "Discord:HomeGuildId is not set, so the editor commands ({Commands}) are "
                    + "registered nowhere and appear in no server at all.",
                    Names(editorModules));
            }

            foreach ((ulong guildId, List<ModuleInfo> modules) in guilds)
            {
                await RegisterToGuildAsync(guildId, modules);
            }

            if (_options.TestGuildIds.Count > 0)
            {
                await ClearGlobalCommandsAsync();
            }
            else
            {
                await RegisterGloballyAsync(openModules);
            }

            WarnIfNotInHomeGuild();
            ExplainEditorVisibility(editorModules);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registering slash commands failed.");
        }
    }

    /// <summary>
    /// Makes one server's commands exactly <paramref name="modules"/>.
    /// </summary>
    /// <remarks>
    /// A failure is logged and swallowed per server: one server Stanley cannot write to must
    /// not stop the others, and the bot is worth running with the commands it did register.
    /// </remarks>
    private async Task RegisterToGuildAsync(ulong guildId, List<ModuleInfo> modules)
    {
        try
        {
            await interactions.AddModulesToGuildAsync(guildId, deleteMissing: true, [.. modules]);

            logger.LogInformation(
                "Registered {Count} command(s) to server {GuildId}: {Commands}.",
                modules.Sum(module => module.SlashCommands.Count),
                guildId,
                Names(modules));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Registering commands to server {GuildId} failed. Stanley has to be able to see "
                + "that server: check the ID, and that the app was invited there.",
                guildId);
        }
    }

    /// <summary>Makes the global commands exactly <paramref name="modules"/>.</summary>
    private async Task RegisterGloballyAsync(ModuleInfo[] modules)
    {
        try
        {
            await interactions.AddModulesGloballyAsync(deleteMissing: true, modules);

            logger.LogInformation(
                "Registered {Count} command(s) globally: {Commands}.",
                modules.Sum(module => module.SlashCommands.Count),
                Names(modules));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Registering the global slash commands failed.");
        }
    }

    /// <summary>
    /// Removes the global commands while <c>Discord:TestGuildIds</c> is set.
    /// </summary>
    /// <remarks>
    /// Without this, a command registered globally by an earlier run stays registered for ever,
    /// because a run with test servers writes only to them. Discord goes on offering it beside
    /// the test server's own copy, and running it reaches a bot that has no handler for it.
    /// Registering to the test servers means the test servers and nowhere else.
    /// </remarks>
    private async Task ClearGlobalCommandsAsync()
    {
        try
        {
            IReadOnlyCollection<SocketApplicationCommand> existing =
                await client.GetGlobalApplicationCommandsAsync();

            if (existing.Count == 0)
            {
                return;
            }

            await client.BulkOverwriteGlobalApplicationCommandsAsync([]);

            logger.LogInformation(
                "Removed {Count} global command(s) ({Commands}): while Discord:TestGuildIds is "
                + "set, commands live in the test servers only.",
                existing.Count,
                string.Join(", ", existing.Select(command => "/" + command.Name)));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Removing the global slash commands failed, so any an earlier run registered are "
                + "still offered everywhere and running one reaches no handler.");
        }
    }

    /// <summary>
    /// Says what still has to be done in Discord before an editor can see the editor commands.
    /// </summary>
    /// <remarks>
    /// They carry <c>DefaultMemberPermissions</c> of Administrator, so Discord hides them from
    /// everyone else in the home server, Wiki Editors included, until an administrator gives
    /// the role access to Stanley. No configuration here can do that step, and skipping it
    /// looks exactly like a command that was never registered, so it is said at every startup.
    /// </remarks>
    private void ExplainEditorVisibility(ModuleInfo[] editorModules)
    {
        if (editorModules.Length == 0 || _options.HomeGuildId is not ulong homeGuildId)
        {
            return;
        }

        logger.LogInformation(
            "The editor commands ({Commands}) are limited to Administrator, so in server "
            + "{GuildId} only administrators see them until the Wiki Editor role is given "
            + "access under Server Settings, Integrations, Stanley, Manage.",
            Names(editorModules),
            homeGuildId);
    }

    /// <summary>The commands a set of modules holds, as they are typed.</summary>
    private static string Names(IEnumerable<ModuleInfo> modules) =>
        string.Join(
            ", ",
            modules.Select(module =>
                string.IsNullOrEmpty(module.SlashGroupName)
                    ? string.Join(", ", module.SlashCommands.Select(command => "/" + command.Name))
                    : "/" + module.SlashGroupName));

    /// <remarks>
    /// Registering commands to a server only needs the <c>applications.commands</c> scope, so
    /// an app invited without the <c>bot</c> scope has working commands there and no member.
    /// Discord then sends no member data with the interaction and the gateway never caches the
    /// server, so there is no role to check and no way to read one (see
    /// <c>RequireWikiEditorAttribute</c>).
    /// </remarks>
    private void WarnIfNotInHomeGuild()
    {
        if (_options.HomeGuildId is not ulong homeGuildId
            || client.GetGuild(homeGuildId) is not null)
        {
            return;
        }

        logger.LogWarning(
            "Stanley is not a member of the home server {GuildId}, so every editor command "
            + "will refuse everyone however the Wiki Editor role is set. Re-invite the app "
            + "with the bot scope: "
            + "https://discord.com/oauth2/authorize?client_id={ApplicationId}"
            + "&scope=bot+applications.commands&permissions=0",
            homeGuildId,
            client.CurrentUser?.Id);
    }
}
