using Discord;
using Discord.Interactions;
using Stanley.Bot.Preconditions;
using Stanley.Bot.Presentation;
using Stanley.Data.Changes;
using Stanley.Data.Fighters;

namespace Stanley.Bot.Modules;

/// <summary>
/// What the editor commands finish with: the form that writes one signature ability (FR-13),
/// and the buttons that confirm a removal (FR-13, FR-14).
/// </summary>
/// <para>
/// Each handler replaces the message it was pressed on, buttons and all, so a confirmation
/// cannot be answered twice and what is left on screen is what happened.
/// </para>
public sealed class FighterAdminFollowUpModule(FighterAdminRepository fighters)
    : InteractionModuleBase<SocketInteractionContext>
{
    /// <summary>Who is making the change, for the change record (FR-15).</summary>
    private EditorContext Editor => EditorContexts.Of(Context);

    /// <summary>
    /// The ability form coming back: its custom ID carries the fighter and the SA number, so
    /// nothing is matched by name a second time (FR-13).
    /// </summary>
    [RequireWikiEditor]
    [ModalInteraction(FighterAdminIds.AbilityModalPattern)]
    public async Task AbilityWrittenAsync(string variantId, string ordinal, AbilityModal modal)
    {
        ArgumentNullException.ThrowIfNull(modal);

        if (!FighterAdminIds.TryRead(variantId, out int fighterId)
            || !FighterAdminIds.TryRead(ordinal, out int saNumber)
            || saNumber is < 1 or > short.MaxValue)
        {
            // Only reachable if the interaction did not come from a form Stanley built.
            await RespondAsync(
                AdminReplies.StaleFighter,
                ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }

        AbilitySetResult result = await fighters.SetAbilityAsync(
            fighterId,
            (short)saNumber,
            modal.Text,
            Editor);

        await RespondAsync(
            AdminReplies.AbilityWritten(result),
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>Confirmed: remove the fighter, its abilities and its aliases (FR-14).</summary>
    [RequireWikiEditor]
    [ComponentInteraction(FighterAdminIds.RemoveFighterPattern)]
    public async Task RemoveFighterAsync(string variantId)
    {
        if (!FighterAdminIds.TryRead(variantId, out int fighterId))
        {
            await ReplaceAsync(AdminReplies.StaleFighter);
            return;
        }

        FighterRemovalResult result = await fighters.RemoveAsync(fighterId, Editor);

        await ReplaceAsync(AdminReplies.FighterRemoved(result));
    }

    /// <summary>Confirmed: remove every signature ability the fighter has (FR-13).</summary>
    [RequireWikiEditor]
    [ComponentInteraction(FighterAdminIds.RemoveAbilitiesPattern)]
    public async Task RemoveAbilitiesAsync(string variantId)
    {
        if (!FighterAdminIds.TryRead(variantId, out int fighterId))
        {
            await ReplaceAsync(AdminReplies.StaleFighter);
            return;
        }

        AbilityRemovalResult result = await fighters.RemoveAbilitiesAsync(fighterId, Editor);

        await ReplaceAsync(AdminReplies.AbilitiesRemoved(result));
    }

    /// <summary>
    /// Cancelled. Nothing was read and nothing was written, so there is nothing to undo.
    /// </summary>
    [RequireWikiEditor]
    [ComponentInteraction(FighterAdminIds.Cancel)]
    public Task CancelAsync() => ReplaceAsync(AdminReplies.Cancelled);

    /// <summary>
    /// Replaces the confirmation the editor pressed with what came of it, and takes the buttons
    /// away so it cannot be pressed again.
    /// </summary>
    private async Task ReplaceAsync(string text)
    {
        if (Context.Interaction is IComponentInteraction button)
        {
            await button.UpdateAsync(message =>
            {
                message.Content = text;
                message.Components = MessageComponent.Empty;
                message.AllowedMentions = AllowedMentions.None;
            });

            return;
        }

        await RespondAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
    }
}
