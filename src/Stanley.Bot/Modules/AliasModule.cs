using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;
using Stanley.Data.Aliases;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/alias</c>: the other names a fighter is known by, listed for everyone (FR-09).
/// </summary>
/// <remarks>
/// <para>
/// Reading only, and registered globally. Adding and removing an alias are
/// <see cref="AliasAdminModule"/>, a command of its own so that the editor half can be
/// registered to the home server alone.
/// </para>
/// <para>
/// A thin handler, as every module is: the matching lives in <see cref="AliasRepository"/>,
/// which is tested without Discord, and the wording lives in <see cref="Replies"/>.
/// </para>
/// </remarks>
[Group("alias", "The other names a fighter is known by.")]
public sealed class AliasModule(AliasRepository aliases)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxNameLength = 100;

    [SlashCommand("list", "Show the other names a fighter is known by.")]
    public async Task ListAsync(
        [Summary("fighter", "The fighter's name. Pick one of the suggestions as you type.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string fighter)
    {
        AliasListing? listing = await aliases.ListAsync(fighter);

        if (listing is null)
        {
            await RespondAsync(
                Replies.NoMatch(fighter),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync(
            Replies.AliasList(listing.FighterName, listing.Aliases),
            allowedMentions: AllowedMentions.None);
    }
}
