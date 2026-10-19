using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/fighter</c>: the fighter card (FR-01 to FR-06, FR-26).
/// </summary>
/// <remarks>
/// A thin handler, as every module is: the matching lives in <see cref="FighterLookup"/> and
/// the card in <see cref="FighterCardBuilder"/>, both of which are testable without Discord.
/// </remarks>
public sealed class FighterModule(FighterLookup lookup, FighterCardBuilder cards)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxNameLength = 100;

    [SlashCommand("fighter", "Show a fighter's card.")]
    public async Task FighterAsync(
        [Summary("name", "The fighter's name. Pick one of the suggestions as you type.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string name)
    {
        FighterCard? card = await lookup.FindCardAsync(name);

        if (card is null)
        {
            // Private, so a mistyped name does not clutter the channel.
            await RespondAsync(
                Replies.NoMatch(name),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        IReadOnlyList<FighterCardMessage> messages = cards.Build(card);

        await RespondAsync(
            text: messages[0].Text,
            embed: messages[0].Embed,
            allowedMentions: AllowedMentions.None);

        // A Raid Boss with more abilities than one message holds continues here
        foreach (FighterCardMessage overflow in messages.Skip(1))
        {
            await FollowupAsync(
                text: overflow.Text,
                embed: overflow.Embed,
                allowedMentions: AllowedMentions.None);
        }
    }
}
