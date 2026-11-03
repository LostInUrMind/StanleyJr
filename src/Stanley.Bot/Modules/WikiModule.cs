using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;
using Stanley.Core.Wiki;
using Stanley.Data.Lookup;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/wiki</c>: a link to a fighter's wiki page, or to one of the wiki pages that are not
/// fighters.
/// </summary>
/// <remarks>
/// A fighter's URL is built in <see cref="WikiUrls"/> and the name is matched by
/// <see cref="FighterLookup"/>, the same matcher <c>/fighter</c> uses, so the two commands
/// cannot disagree about what a name means. The pages that are not fighters have no name to
/// build from, so <see cref="WikiPageRepository"/> holds a whole URL for each; editors
/// maintain them with <c>/wiki-admin</c>.
/// </remarks>
public sealed class WikiModule(WikiPageRepository pages, FighterLookup lookup)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxNameLength = 100;

    [SlashCommand("wiki", "Link a fighter's wiki page.")]
    public async Task WikiAsync(
        [Summary("name", "A fighter, or another wiki page. Pick one of the suggestions.")]
        [Autocomplete(typeof(WikiPageAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string name)
    {
        // Resolved first because these pages are not fighters, so the fighter lookup can never
        // return them. The match is exact, which is why it cannot shadow a fighter whose name
        // merely resembles a key.
        if (await pages.FindUrlAsync(name) is string url)
        {
            await RespondAsync(url, allowedMentions: AllowedMentions.None);
            return;
        }

        WikiTarget? target = await lookup.FindWikiTargetAsync(name);

        if (target is null)
        {
            await RespondAsync(
                Replies.NoWikiMatch(name),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync(
            WikiUrls.UrlFor(target.Name, target.StoredUrl),
            allowedMentions: AllowedMentions.None);
    }
}
