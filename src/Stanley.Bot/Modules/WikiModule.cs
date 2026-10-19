using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;
using Stanley.Core.Wiki;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/wiki</c>: a link to a fighter's wiki page (FR-07), or to one of the wiki pages that are
/// not fighters (FR-08).
/// </summary>
/// <remarks>
/// The URL is built in <see cref="WikiPages"/> and the name is matched by
/// <see cref="FighterLookup"/>, the same matcher <c>/fighter</c> uses, so the two commands
/// cannot disagree about what a name means (FR-07).
/// </remarks>
public sealed class WikiModule(FighterLookup lookup)
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
        // Resolved first because these pages have no database row, so the fighter lookup can
        // never return them (FR-08). 
        if (WikiPages.Find(name) is WikiExtraPage page)
        {
            await RespondAsync(page.Url, allowedMentions: AllowedMentions.None);
            return;
        }

        WikiTarget? target = await lookup.FindWikiTargetAsync(name);

        if (target is null)
        {
            await RespondAsync(
                Replies.NoMatch(name),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync(
            WikiPages.UrlFor(target.Name, target.StoredUrl),
            allowedMentions: AllowedMentions.None);
    }
}
