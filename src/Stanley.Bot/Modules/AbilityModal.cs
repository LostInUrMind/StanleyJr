using Discord;
using Discord.Interactions;
using Stanley.Bot.Presentation;
using Stanley.Data.Entities;
using Stanley.Data.Fighters;

namespace Stanley.Bot.Modules;

/// <summary>
/// The pop-up form that writes one signature ability.
/// </summary>
/// <remarks>
/// One ability per form, with one field in it. A Discord form holds at most five components and
/// a fighter has no fixed number of abilities — the Raid Bosses have five — so a form per
/// ability is the only shape that works for every fighter (database design section 3.3). It
/// doubles as the only place an editor can type 1,024 characters: a command option is a single
/// line.
/// </remarks>
public sealed class AbilityModal : IModal
{
    /// <summary>
    /// The field's own ID. 
    /// </summary>
    public const string TextInputId = "ability-text";

    /// <summary>
    /// The fallback title. <see cref="Build"/> replaces it with one naming the fighter and the
    /// SA number, which is the only thing telling two open forms apart.
    /// </summary>
    public string Title => "Signature ability";

    [InputLabel("Ability text")]
    [ModalTextInput(
        TextInputId,
        TextInputStyle.Paragraph,
        maxLength: SignatureAbility.MaxTextLength)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The form for <paramref name="prompt"/>, pre-filled with the ability's current text when
    /// it has one, which is what FR-13 requires of an edit.
    /// </summary>
    public static Modal Build(AbilityPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        return new ModalBuilder()
            .WithTitle(AdminReplies.AbilityModalTitle(prompt))
            .WithCustomId(FighterAdminIds.AbilityModal(prompt.VariantId, prompt.Ordinal))
            .AddTextInput(
                label: "Ability text",
                customId: TextInputId,
                style: TextInputStyle.Paragraph,
                placeholder: "What the ability does, as the card shows it.",
                maxLength: SignatureAbility.MaxTextLength,
                required: true,
                value: prompt.CurrentText)
            .Build();
    }
}
