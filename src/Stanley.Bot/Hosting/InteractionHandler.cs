using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stanley.Bot.Presentation;

namespace Stanley.Bot.Hosting;

/// <summary>
/// Finds the command modules and runs the right one for each interaction.
/// </summary>
/// <remarks>
/// A new command is a method on a module in <c>Modules/</c>; nothing here changes
/// (NFR-07). Handlers are deliberately thin: the logic they call lives in Stanley.Core so it
/// can be tested without Discord.
/// </remarks>
internal sealed class InteractionHandler(
    DiscordSocketClient client,
    InteractionService interactions,
    IServiceProvider services,
    ILogger<InteractionHandler> logger)
{
    /// <summary>Registers every module in this assembly and starts listening.</summary>
    public async Task InitializeAsync()
    {
        await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);

        client.InteractionCreated += OnInteractionCreatedAsync;

        // All three report the same way. Buttons and pop-up forms are interactions like any
        // other, and a refusal or a failure in one has to reach the user who pressed it rather
        // than only the log (FRD section 6.2).
        interactions.SlashCommandExecuted += OnExecutedAsync;
        interactions.ComponentCommandExecuted += OnExecutedAsync;
        interactions.ModalCommandExecuted += OnExecutedAsync;
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);
            await interactions.ExecuteCommandAsync(context, services);
        }
        catch (Exception ex)
        {
            // Nothing is allowed to take the gateway connection down with it.
            logger.LogError(ex, "Unhandled failure while dispatching an interaction.");
            await RespondWithFailureAsync(interaction);
        }
    }

    private async Task OnExecutedAsync(
        ICommandInfo? command,
        IInteractionContext context,
        IResult result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        // Discord.Net reports an interaction that matched no handler with a null command, so
        // this has to be checked before the command is read at all. It means Discord is still
        // offering something this build does not have: a command removed or renamed in code,
        // which Discord keeps offering until its registration is overwritten (and keeps
        // showing in the picker for a while after that, from the client's own cache).
        if (command is null || result.Error == InteractionCommandError.UnknownCommand)
        {
            logger.LogWarning(
                "An interaction arrived for a command this build does not have: {Reason}. "
                + "Discord still has an old registration; it goes away once the bot has "
                + "started with the current commands.",
                result.ErrorReason);

            await RespondPrivatelyAsync(context.Interaction, Replies.CommandGone);
            return;
        }

        string name = Describe(command);

        // A precondition refusing the caller is a normal outcome, not a bug.
        if (result.Error == InteractionCommandError.UnmetPrecondition)
        {
            logger.LogInformation(
                "{Command} was refused for user {UserId}: {Reason}",
                name,
                context.User.Id,
                result.ErrorReason);

            await RespondPrivatelyAsync(context.Interaction, result.ErrorReason);
            return;
        }

        logger.LogError(
            "{Command} failed for user {UserId}: {Error} {Reason}",
            name,
            context.User.Id,
            result.Error,
            result.ErrorReason);

        await RespondWithFailureAsync(context.Interaction);
    }

    /// <summary>
    /// What the log calls the handler: a slash command by the command the user typed, and a
    /// button or a form by the custom ID it matched, which is what identifies it.
    /// </summary>
    /// <remarks>
    /// Null-tolerant on purpose. The callers above handle that case themselves, and this is
    /// reached on every failure path, so it must not be the thing that throws while reporting
    /// one: an exception here is raised inside Discord.Net's event, not in the command, and
    /// leaves the user with no reply at all.
    /// </remarks>
    internal static string Describe(ICommandInfo? command) => command switch
    {
        null => "An unknown command",
        SlashCommandInfo slash => $"/{slash.Name}",
        _ => command.Name,
    };

    /// <summary>
    /// Tells the user something went wrong, privately, without leaking the reason.
    /// </summary>
    private static Task RespondWithFailureAsync(IDiscordInteraction interaction) =>
        RespondPrivatelyAsync(
            interaction,
            "Something went wrong running that command. Please try again.");

    private static async Task RespondPrivatelyAsync(
        IDiscordInteraction interaction,
        string message)
    {
        try
        {
            if (interaction.HasResponded)
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
        }
        catch (Exception)
        {
            // The interaction may already have expired; there is nowhere left to reply.
        }
    }
}
