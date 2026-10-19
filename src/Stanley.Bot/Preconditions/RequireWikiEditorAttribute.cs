using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stanley.Bot.Configuration;

namespace Stanley.Bot.Preconditions;

/// <summary>
/// Refuses a command unless the caller holds the Wiki Editor role in the home server.
/// </summary>
/// <remarks>
/// <para>
/// Both the server and the role are matched by ID. Anyone can create a role called "Wiki
/// Editor" in their own server, so a name check, or a role check in whichever server the
/// command was run in, would hand every server its own editors.
/// </para>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireWikiEditorAttribute : PreconditionAttribute
{
    /// <summary>
    /// Shown when the role has not been configured yet. 
    /// </summary>
    internal const string NotConfigured =
        "Editing is not set up yet. An administrator has to configure the Wiki Editor role before Stanley's data can be changed.";

    /// <summary>
    /// Shown for every other refusal.
    /// </summary>
    internal const string NotAnEditor = "Only Wiki Editors can change Stanley's data.";

    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(services);

        DiscordOptions options = services.GetRequiredService<IOptions<DiscordOptions>>().Value;

        ulong? guildId = context.Interaction?.GuildId;

        IReadOnlyCollection<ulong>? roleIds = guildId is not null && guildId == options.HomeGuildId
            ? await RoleIdsAsync(context, guildId.Value, services)
            : null;

        return Check(options, guildId, roleIds);
    }

    /// <summary>
    /// The caller's roles in <paramref name="guildId"/>: from the interaction when it carried
    /// the member, and otherwise from one REST read of that member. Null when they cannot be
    /// established, which is a refusal.
    /// </summary>
    private static async Task<IReadOnlyCollection<ulong>?> RoleIdsAsync(
        IInteractionContext context,
        ulong guildId,
        IServiceProvider services)
    {
        if (context.User is IGuildUser member)
        {
            return member.RoleIds;
        }

        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<RequireWikiEditorAttribute>();
        var client = services.GetRequiredService<DiscordSocketClient>();

        try
        {
            RestGuildUser? fetched =
                await client.Rest.GetGuildUserAsync(guildId, context.User.Id);

            if (fetched is null)
            {
                logger.LogWarning(
                    "User {UserId} is not a member of guild {GuildId}, so the command was refused.",
                    context.User.Id,
                    guildId);

                return null;
            }

            logger.LogDebug(
                "The interaction carried no member, so user {UserId}'s roles in guild {GuildId} were read over REST.",
                context.User.Id,
                guildId);

            return fetched.RoleIds;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Reading user {UserId}'s roles in guild {GuildId} failed, so the command was "
                + "refused. Stanley has to be a member of that server, invited with the bot "
                + "scope, for a role to be checked at all.",
                context.User.Id,
                guildId);

            return null;
        }
    }

    internal static PreconditionResult Check(
        DiscordOptions options,
        ulong? guildId,
        IReadOnlyCollection<ulong>? roleIds)
    {
        if (options is null || options.HomeGuildId is not ulong homeGuildId || options.WikiEditorRoleId is not ulong editorRoleId)
        {
            return PreconditionResult.FromError(NotConfigured);
        }

        if (guildId != homeGuildId || roleIds is null || !roleIds.Contains(editorRoleId))
        {
            return PreconditionResult.FromError(NotAnEditor);
        }

        return PreconditionResult.FromSuccess();
    }
}
