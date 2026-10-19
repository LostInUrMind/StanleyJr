using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Bot.Presentation;
using Stanley.Data.Lookup;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions Discord shows while someone types a fighter name (FR-03).
/// </summary>
/// <remarks>
/// Fighters only. <c>/wiki</c> also offers pages that are not fighters and uses
/// <see cref="WikiPageAutocompleteHandler"/> for that.
/// </remarks>
public sealed class FighterNameAutocompleteHandler : SuggestionAutocompleteHandler
{
    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var lookup = services.GetRequiredService<FighterLookup>();

        IReadOnlyList<FighterSuggestion> suggestions = await lookup.SuggestAsync(typed);

        return suggestions.Select(suggestion => new AutocompleteResult(
            Replies.ChoiceLabel(suggestion),
            suggestion.FighterSlug));
    }
}
