using Discord;
using Discord.Interactions;
using Stanley.Bot.Preconditions;
using Stanley.Bot.Presentation;
using Stanley.Data.Changes;
using Stanley.Data.Fighters;

namespace Stanley.Bot.Modules;

/// <summary>
/// <c>/fighter-admin</c>: the editor commands that keep the fighter data right — adding a
/// fighter (FR-12), changing one (FR-13), writing its signature abilities (FR-13) and removing
/// it (FR-14).
/// </summary>
/// <remarks>
/// <para>
/// A thin handler, as every module is: the rules, the change records and every refusal live in
/// <see cref="FighterAdminRepository"/>, which is tested without Discord, and the wording lives
/// in <see cref="AdminReplies"/>.
/// </para>
/// <para>
/// <c>[DontAutoRegister]</c> keeps this command out of the global registration, so
/// <see cref="Hosting.DiscordBotService"/> can register it to the home server alone and it does
/// not appear anywhere else (migration plan section 6). The precondition is still the check
/// that matters: a command registered to one server is only hidden elsewhere, not refused.
/// </para>
/// <para>
/// Two of the four subcommands finish somewhere else. A removal asks first, so it ends on a
/// button, and an ability's text needs a form; both are handled in
/// <see cref="FighterAdminFollowUpModule"/>, which has to be a module of its own because this
/// one's group prefix would otherwise be part of the custom IDs it matches.
/// </para>
/// </remarks>
[Group("fighter-admin", "Editor commands for Stanley's fighter data.")]
[RequireWikiEditor]
[DefaultMemberPermissions(GuildPermission.Administrator)]
[DontAutoRegister]
public sealed class FighterAdminModule(FighterAdminRepository fighters)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>The longest string option Discord will send.</summary>
    private const int MaxNameLength = 100;

    /// <summary>
    /// Long enough for any quote in the game, short enough that a paste of something else is
    /// refused before it reaches the card.
    /// </summary>
    private const int MaxQuoteLength = 500;

    /// <summary>Long enough for the portrait URLs, which are deep GitHub paths.</summary>
    private const int MaxUrlLength = 500;

    /// <summary>Who is making the change, for the change record (FR-15).</summary>
    private EditorContext Editor => EditorContexts.Of(Context);

    [SlashCommand(
        "add",
        "Add a fighter. Give it its abilities afterwards with /fighter-admin ability.")]
    public async Task AddAsync(
        [Summary("name", "The fighter's name, as the card should show it.")]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string name,
        [Summary("character", "The character this fighter is a variant of. Pick a suggestion.")]
        [Autocomplete(typeof(CharacterAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string character,
        [Summary("rarity", "The fighter's rarity.")] FighterRarity rarity,
        [Summary("element", "The fighter's element.")] FighterElement element,
        [Summary("ability-name", "The signature ability name, for example RIFF WRATH.")]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string abilityName,
        [Summary("atk", "Base attack. A whole number greater than zero.")]
        [MinValue(1)]
        int atk,
        [Summary("hp", "Base health. A whole number greater than zero.")]
        [MinValue(1)]
        int hp,
        [Summary("fs", "Base fighter score. A whole number greater than zero.")]
        [MinValue(1)]
        int fs,
        [Summary(
            "portrait-url",
            "Web address of the fighter's portrait. Stanley links it, never hosts it.")]
        [MinLength(1)]
        [MaxLength(MaxUrlLength)]
        string portraitUrl)
    {
        // The rarity and the element travel as their names: the repository matches them against
        // the seeded rows, so a list that has drifted from the table is a refusal, not a guess.
        var draft = new FighterDraft(
            name,
            character,
            rarity.ToString(),
            element.ToString(),
            abilityName,
            atk,
            hp,
            fs,
            portraitUrl);

        FighterWriteResult result = await fighters.AddAsync(draft, Editor);

        await RespondAsync(
            AdminReplies.FighterAdded(result, name),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("edit", "Change a fighter. Give only the fields you want to change.")]
    public async Task EditAsync(
        [Summary("fighter", "The fighter to change. Pick one of the suggestions.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string fighter,
        [Summary("name", "A new name. The fighter stays findable by its aliases.")]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string? name = null,
        [Summary("character", "The character this fighter is a variant of. Pick a suggestion.")]
        [Autocomplete(typeof(CharacterAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string? character = null,
        [Summary("rarity", "The fighter's rarity.")] FighterRarity? rarity = null,
        [Summary("element", "The fighter's element.")] FighterElement? element = null,
        [Summary("ability-name", "The signature ability name.")]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string? abilityName = null,
        [Summary("atk", "Base attack. A whole number greater than zero.")]
        [MinValue(1)]
        int? atk = null,
        [Summary("hp", "Base health. A whole number greater than zero.")]
        [MinValue(1)]
        int? hp = null,
        [Summary("fs", "Base fighter score. A whole number greater than zero.")]
        [MinValue(1)]
        int? fs = null,
        [Summary("portrait-url", "Web address of the fighter's portrait.")]
        [MinLength(1)]
        [MaxLength(MaxUrlLength)]
        string? portraitUrl = null,
        [Summary("wiki-url", "Overrides the wiki link, for a page whose title differs.")]
        [MinLength(1)]
        [MaxLength(MaxUrlLength)]
        string? wikiUrl = null,
        [Summary("quote", "The fighter's quote.")]
        [MinLength(1)]
        [MaxLength(MaxQuoteLength)]
        string? quote = null,
        [Summary("raid-boss", "Whether this is a non-playable Raid Boss, left out of the counts.")]
        bool? raidBoss = null)
    {
        var edit = new FighterEdit(
            name,
            character,
            rarity?.ToString(),
            element?.ToString(),
            abilityName,
            atk,
            hp,
            fs,
            portraitUrl,
            wikiUrl,
            quote,
            raidBoss);

        FighterWriteResult result = await fighters.EditAsync(fighter, edit, Editor);

        await RespondAsync(
            AdminReplies.FighterEdited(result, fighter),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// Writes one signature ability, or removes the whole block (FR-13).
    /// </summary>
    [SlashCommand("ability", "Write one signature ability, or remove every one a fighter has.")]
    public async Task AbilityAsync(
        [Summary("fighter", "The fighter whose abilities these are. Pick one of the suggestions.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string fighter,
        [Summary("action", "Write one ability, or remove every one the fighter has.")]
        AbilityAction action,
        [Summary(
            "sa",
            "Which ability to write, for example 1 for SA1. Leave it out to add one at the end.")]
        [MinValue(1)]
        int? sa = null)
    {
        if (action is AbilityAction.RemoveAll)
        {
            await AskToRemoveAbilitiesAsync(fighter, sa);
            return;
        }

        AbilityPrompt prompt = await fighters.PrepareAbilityAsync(fighter, sa);

        if (prompt.Outcome is not FighterWriteOutcome.Ok)
        {
            await RespondAsync(
                AdminReplies.AbilityNotPrompted(prompt, fighter),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        // The form is the response to this command, so the three seconds of NFR-01 are spent on
        // the one read above; the write happens when the form comes back.
        await RespondWithModalAsync(AbilityModal.Build(prompt));
    }

    [SlashCommand("remove", "Remove a fighter, with its abilities and aliases.")]
    public async Task RemoveAsync(
        [Summary("fighter", "The fighter to remove. Pick one of the suggestions.")]
        [Autocomplete(typeof(FighterNameAutocompleteHandler))]
        [MinLength(1)]
        [MaxLength(MaxNameLength)]
        string fighter)
    {
        FighterTarget? target = await fighters.FindTargetAsync(fighter);

        if (target is null)
        {
            await RespondAsync(
                Replies.NoMatch(fighter),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        // Asked first, never done first: FR-14 requires a confirmation step, and this one says
        // what goes with the fighter before anything is removed.
        await RespondAsync(
            AdminReplies.ConfirmRemoveFighter(target),
            ephemeral: true,
            components: Confirmation(
                FighterAdminIds.RemoveFighter(target.VariantId),
                "Remove fighter"),
            allowedMentions: AllowedMentions.None);
    }

    private async Task AskToRemoveAbilitiesAsync(string fighter, int? sa)
    {
        if (sa is not null)
        {
            await RespondAsync(
                AdminReplies.SaNumberNeedsSet,
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        FighterTarget? target = await fighters.FindTargetAsync(fighter);

        if (target is null)
        {
            await RespondAsync(
                Replies.NoMatch(fighter),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        // Nothing to confirm when there is nothing to remove.
        if (target.AbilityCount == 0)
        {
            await RespondAsync(
                AdminReplies.NoAbilities(target.Name),
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        await RespondAsync(
            AdminReplies.ConfirmRemoveAbilities(target),
            ephemeral: true,
            components: Confirmation(
                FighterAdminIds.RemoveAbilities(target.VariantId),
                "Remove all abilities"),
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// The confirm and cancel buttons of a destructive command (FRD section 6.1).
    /// </summary>
    /// <remarks>
    /// The confirmation is a private reply, so only the editor who ran the command can see the buttons.
    /// </remarks>
    private static MessageComponent Confirmation(string confirmId, string confirmLabel) =>
        new ComponentBuilder()
            .WithButton(confirmLabel, confirmId, ButtonStyle.Danger)
            .WithButton("Cancel", FighterAdminIds.Cancel, ButtonStyle.Secondary)
            .Build();
}
