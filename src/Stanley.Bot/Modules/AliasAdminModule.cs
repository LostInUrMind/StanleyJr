using Discord;
using Discord.Interactions;
using Stanley.Bot.Preconditions;
using Stanley.Bot.Presentation;
using Stanley.Data.Aliases;
using Stanley.Data.Changes;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/alias-admin</c>: the editor commands that add and remove the other names a fighter is
/// known by (FR-10, FR-11). Listing them is <c>/alias list</c>, which everyone can run.
/// </summary>
/// <remarks>
/// <para>
/// A thin handler, as every module is: the matching, the rules and the change records live in
/// <see cref="AliasRepository"/>, which is tested without Discord, and the wording lives in
/// <see cref="Replies"/>.
/// </para>
/// </remarks>
[Group("alias-admin", "Editor commands for the other names a fighter is known by.")]
[RequireWikiEditor]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[DontAutoRegister]
public sealed class AliasAdminModule(AliasRepository aliases)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxNameLength = 100;

    /// <summary>Who is making the change, for the change record.</summary>
    private EditorContext Editor => EditorContexts.Of(Context);

    [SlashCommand("add", "Add another name for a fighter.")]
    public async Task AddAsync(
        [Summary("fighter", "The fighter to add the name to. Pick one of the suggestions.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string fighter,
        [Summary("alias", "The name players use for that fighter.")]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string alias)
    {
        AliasAddResult result = await aliases.AddAsync(fighter, alias, Editor);

        string reply = result switch
        {
            { Outcome: AliasAddOutcome.Added, FighterName: string fighterName } =>
                Replies.AliasAdded(result.Alias, fighterName),
            { Outcome: AliasAddOutcome.AliasTaken, FighterName: string owner } =>
                Replies.AliasTaken(result.Alias, owner),
            { Outcome: AliasAddOutcome.FighterNameTaken, FighterName: string owner } =>
                Replies.AliasIsFighterName(result.Alias, owner),
            { Outcome: AliasAddOutcome.AliasNotUsable } =>
                Replies.AliasNotUsable(result.Alias),
            _ => Replies.NoMatch(fighter),
        };

        await RespondAsync(reply, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("remove", "Remove one of a fighter's other names.")]
    public async Task RemoveAsync(
        [Summary("alias", "The alias to remove. Pick one of the suggestions.")]
        [Autocomplete(typeof(AliasAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string alias)
    {
        RemovedAlias? removed = await aliases.RemoveAsync(alias, Editor);

        string reply = removed is null
            ? Replies.NoAliasMatch(alias)
            : Replies.AliasRemoved(removed.Alias, removed.FighterName);

        await RespondAsync(reply, ephemeral: true, allowedMentions: AllowedMentions.None);
    }
}
