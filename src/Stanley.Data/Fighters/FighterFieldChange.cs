namespace Stanley.Data.Fighters;

/// <summary>
/// One editable part of a fighter, named structurally so a refusal or a confirmation can say
/// which option it is about without the data layer holding any wording.
/// </summary>
public enum FighterField
{
    Name,
    Character,
    Rarity,
    Element,
    AbilityName,
    BaseAtk,
    BaseHp,
    BaseFs,
    PortraitUrl,
    WikiUrl,
    Quote,
    RaidBoss,
    AbilityText,
}

/// <summary>
/// One field an edit changed, before and after, which is what the private confirmation
/// has to show.
/// </summary>
/// <remarks>
/// The values are the stored values as text: a number formatted invariantly, a flag as
/// <c>true</c> or <c>false</c>, a reference row as its name. The reply layer words them. This
/// pair is for the editor reading the confirmation; what an undo reads is the JSON snapshot in
/// the change record.
/// </remarks>
/// <param name="Field">Which field changed.</param>
/// <param name="Before">Its stored value, or null when it was not set.</param>
/// <param name="After">Its new value.</param>
public sealed record FighterFieldChange(FighterField Field, string? Before, string? After);
