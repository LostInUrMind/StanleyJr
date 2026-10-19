using Discord.Interactions;
using Discord.WebSocket;

namespace Stanley.Bot.Modules;

/// <summary>
/// The smallest possible command, and the template for the rest: one attributed method per
/// command, in a module class, with no logic of its own.
/// </summary>
public sealed class PingModule(DiscordSocketClient client)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Check that Stanley is awake.")]
    public Task PingAsync() =>
        // Private reply: a health check is for the person who asked. So no ping spam.
        RespondAsync($"Stanley is awake. Gateway latency: {client.Latency} ms.", ephemeral: true);
}
