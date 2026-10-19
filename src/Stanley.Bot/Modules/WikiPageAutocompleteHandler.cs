using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Bot.Presentation;
using Stanley.Core.Wiki;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions for <c>/wiki</c>: every fighter, plus the wiki pages that are not fighters
/// (FR-03, FR-08).
/// </summary>
public sealed class WikiPageAutocompleteHandler : SuggestionAutocompleteHandler
{
    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var lookup = services.GetRequiredService<FighterLookup>();

        // The non-fighter pages go first. 
        var choices = WikiPages
            .Suggest(typed)
            .Select(page => new AutocompleteResult(page.Title, page.Slug))
            .ToList();

        IReadOnlyList<FighterSuggestion> fighters = await lookup.SuggestAsync(typed);

        choices.AddRange(fighters.Select(suggestion => new AutocompleteResult(
            Replies.ChoiceLabel(suggestion),
            suggestion.FighterSlug)));

        return choices;
    }
}
