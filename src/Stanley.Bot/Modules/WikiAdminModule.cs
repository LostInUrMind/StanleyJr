using Discord;
using Discord.Interactions;
using Stanley.Bot.Preconditions;
using Stanley.Bot.Presentation;
using Stanley.Data.Changes;
using Stanley.Data.Wiki;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/wiki-admin</c>: the editor commands for the wiki pages that are not fighters — adding
/// one, correcting one and removing one. Linking them is <c>/wiki</c>, which everyone can run.
/// </summary>
/// <remarks>
/// <para>
/// A thin handler, as every module is: the rules, the change records and every refusal live in
/// <see cref="WikiPageRepository"/>, which is tested without Discord, and the wording lives in
/// <see cref="AdminReplies"/>.
/// </para>
/// <para>
/// <c>[DontAutoRegister]</c> keeps this command out of the global registration, so
/// <see cref="Hosting.DiscordBotService"/> can register it to the home server alone and it
/// does not appear anywhere else. The precondition is still the check that matters: a command
/// registered to one server is only hidden elsewhere, not refused.
/// </para>
/// <para>
/// A page players call by two names is two pages here, sharing one address. That is what lets
/// either name be corrected or dropped on its own, and it is why there is no command for
/// "the page's other names": another name is another <c>/wiki-admin add</c>.
/// </para>
/// </remarks>
[Group("wiki-admin", "Editor commands for the wiki pages that are not fighters.")]
[RequireWikiEditor]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[DontAutoRegister]
public sealed class WikiAdminModule(WikiPageRepository pages)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxKeyLength = 100;

    /// <summary>
    /// Long enough for any wiki address, short enough that a paste of something else is
    /// refused before it is stored. The same cap the fighter URLs use.
    /// </summary>
    private const int MaxUrlLength = 500;

    /// <summary>Who is making the change, for the change record.</summary>
    private EditorContext Editor => EditorContexts.Of(Context);

    [SlashCommand("add", "Add a wiki page that is not a fighter.")]
    public async Task AddAsync(
        [Summary("key", "What a user types in /wiki to get this page.")]
        [MinLength(1)]
        [MaxLength(MaxKeyLength)]
        string key,
        [Summary("url", "The page's full web address, starting with https://.")]
        [MinLength(1)]
        [MaxLength(MaxUrlLength)]
        string url)
    {
        WikiPageWriteResult result = await pages.AddAsync(key, url, Editor);

        await RespondAsync(
            AdminReplies.WikiPageAdded(result, key),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("edit", "Change a wiki page's key, its address, or both.")]
    public async Task EditAsync(
        [Summary("page", "The page to change. Pick one of the suggestions.")]
        [Autocomplete(typeof(WikiPageKeyAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxKeyLength)]
        string page,
        [Summary("key", "A new key for it. Leave this out to keep the key it has.")]
        [MinLength(1)]
        [MaxLength(MaxKeyLength)]
        string? key = null,
        [Summary("url", "A new address for it. Leave this out to keep the address it has.")]
        [MinLength(1)]
        [MaxLength(MaxUrlLength)]
        string? url = null)
    {
        WikiPageWriteResult result = await pages.EditAsync(page, key, url, Editor);

        await RespondAsync(
            AdminReplies.WikiPageEdited(result, page),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("remove", "Remove a wiki page.")]
    public async Task RemoveAsync(
        [Summary("page", "The page to remove. Pick one of the suggestions.")]
        [Autocomplete(typeof(WikiPageKeyAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxKeyLength)]
        string page)
    {
        RemovedWikiPage? removed = await pages.RemoveAsync(page, Editor);

        string reply = removed is null
            ? AdminReplies.NoWikiPageMatch(page)
            : AdminReplies.WikiPageRemoved(removed);

        await RespondAsync(reply, ephemeral: true, allowedMentions: AllowedMentions.None);
    }
}
