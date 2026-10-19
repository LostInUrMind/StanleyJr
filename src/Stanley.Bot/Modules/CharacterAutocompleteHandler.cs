using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Stanley.Data.Fighters;

namespace Stanley.Bot.Modules;

/// <summary>
/// The suggestions for the <c>character</c> option of the editor commands.
/// </summary>
public sealed class CharacterAutocompleteHandler : SuggestionAutocompleteHandler
{
    protected override async Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services)
    {
        var fighters = services.GetRequiredService<FighterAdminRepository>();

        IReadOnlyList<string> names = await fighters.SuggestCharactersAsync(typed);

        // The name is both what is shown and what is sent: a character is named by its name.
        return names.Select(name => new AutocompleteResult(name, name));
    }
}
