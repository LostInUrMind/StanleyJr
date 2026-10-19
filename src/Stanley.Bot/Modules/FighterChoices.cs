using Discord.Interactions;

namespace Stanley.Bot.Modules;

/// <summary>
/// The rarities an editor can pick from.
/// </summary>
public enum FighterRarity
{
    Bronze,
    Silver,
    Gold,
    Diamond,
}

/// <summary>
/// The elements an editor can pick from, matched by name against the six seeded <c>elements</c> rows.
/// </summary>
public enum FighterElement
{
    Fire,
    Water,
    Air,
    Light,
    Dark,
    Neutral,
}

/// <summary>
/// What <c>/fighter-admin ability</c> is being asked to do (FRD section 6.1).
/// </summary>
/// <remarks>
/// Two actions and no more, there is no action that removes a single ability.
/// </remarks>
public enum AbilityAction
{
    /// <summary>Write one ability: a new one at the end of the block, or over an existing one.</summary>
    [ChoiceDisplay("Set one ability")]
    Set,

    /// <summary>Remove every ability the fighter has, after a confirmation.</summary>
    [ChoiceDisplay("Remove all abilities")]
    RemoveAll,
}
