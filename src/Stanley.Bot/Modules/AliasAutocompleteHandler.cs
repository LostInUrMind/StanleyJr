using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Bot.Presentation;
using Stanley.Data.Aliases;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions for <c>/alias-admin remove</c>: the aliases that exist, each with the fighter it
/// belongs to.
/// </summary>
public sealed class AliasAutocompleteHandler : SuggestionAutocompleteHandler
{
    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var aliases = services.GetRequiredService<AliasRepository>();

        IReadOnlyList<AliasSuggestion> suggestions = await aliases.SuggestAsync(typed);

        return suggestions.Select(suggestion => new AutocompleteResult(
            Replies.ChoiceLabel(suggestion),
            suggestion.AliasSlug));
    }
}
