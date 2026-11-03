using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Bot.Presentation;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions for <c>/wiki-admin edit</c> and <c>/wiki-admin remove</c>: the wiki pages
/// that exist, each with the page it points at.
/// </summary>
/// <remarks>
/// Pages only, unlike <see cref="WikiPageAutocompleteHandler"/>. A fighter has no row here, so
/// offering one would suggest a page that cannot be edited or removed.
/// </remarks>
public sealed class WikiPageKeyAutocompleteHandler : SuggestionAutocompleteHandler
{
    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var pages = services.GetRequiredService<WikiPageRepository>();

        IReadOnlyList<WikiPageSuggestion> suggestions = await pages.SuggestAsync(typed);

        return suggestions.Select(suggestion => new AutocompleteResult(
            Replies.ChoiceLabel(suggestion),
            suggestion.KeySlug));
    }
}
