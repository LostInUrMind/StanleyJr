using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Bot.Presentation;
using Stanley.Data.Lookup;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions for <c>/wiki</c>: every fighter, plus the wiki pages that are not fighters.
/// </summary>
public sealed class WikiPageAutocompleteHandler : SuggestionAutocompleteHandler
{
    /// <summary>
    /// How many of the 25 choices the wiki pages may take.
    /// </summary>
    /// <remarks>
    /// Capped, because nothing limits how many pages editors add and the pages are offered
    /// first: without this, enough of them would push the fighters out of the list entirely,
    /// and <c>/wiki</c> is asked for a fighter far more often than for a page. Five is plenty
    /// to recognise the one meant, and a user who wants a particular page types more of it.
    /// </remarks>
    private const int MaxPageChoices = 5;

    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var pages = services.GetRequiredService<WikiPageRepository>();
        var lookup = services.GetRequiredService<FighterLookup>();

        // The non-fighter pages go first, in the order /wiki resolves them.
        IReadOnlyList<WikiPageSuggestion> wikiPages =
            await pages.SuggestAsync(typed, MaxPageChoices);

        var choices = wikiPages
            .Select(page => new AutocompleteResult(page.Key, page.KeySlug))
            .ToList();

        IReadOnlyList<FighterSuggestion> fighters = await lookup.SuggestAsync(typed);

        choices.AddRange(fighters.Select(suggestion => new AutocompleteResult(
            Replies.ChoiceLabel(suggestion),
            suggestion.FighterSlug)));

        return choices;
    }
}
