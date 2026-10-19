using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/credits</c>: who made Stanley (FR-18).
/// </summary>
/// <remarks>
public sealed class CreditsModule(FighterPresentation presentation)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("credits", "Who made Stanley.")]
    public Task CreditsAsync() =>
        RespondAsync(
            Replies.Credits(presentation.FighterScoreIcon),
            allowedMentions: AllowedMentions.None);
}
