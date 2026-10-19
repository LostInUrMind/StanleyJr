using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Stanley.Bot.Modules;

/// <summary>
/// The shared half of every autocomplete: read what the user has typed, cap the choices at
/// what Discord accepts, and never let a failure leave the box spinning.
/// </summary>
/// <remarks>
/// A handler supplies only <see cref="SuggestAsync"/>. Autocomplete cannot be deferred and has
/// no way to show an error, so a failure becomes an empty list and the user can still type the
/// name in full.
/// </remarks>
public abstract class SuggestionAutocompleteHandler : AutocompleteHandler
{
    /// <summary>The most choices Discord will show (FR-03).</summary>
    private const int MaxChoices = 25;

    public sealed override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(autocompleteInteraction);
        ArgumentNullException.ThrowIfNull(services);

        string typed = autocompleteInteraction.Data.Current.Value as string ?? string.Empty;

        try
        {
            IEnumerable<AutocompleteResult> choices = await SuggestAsync(typed, services);

            return AutocompletionResult.FromSuccess(choices.Take(MaxChoices));
        }
        catch (Exception ex)
        {
            ILogger logger = services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(GetType());

            logger.LogError(ex, "Suggesting choices for {Typed} failed.", typed);

            return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
        }
    }

    /// <summary>
    /// The choices for what has been typed so far, best first. More than
    /// <see cref="MaxChoices"/> may be returned; the extras are dropped.
    /// </summary>
    protected abstract Task<IEnumerable<AutocompleteResult>> SuggestAsync(
        string typed,
        IServiceProvider services);
}
