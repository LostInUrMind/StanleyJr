using System.Globalization;

namespace Stanley.Bot.Modules;

/// <summary>
/// The custom IDs of the buttons and forms the editor commands open, and the patterns their
/// handlers match.
/// </summary>
/// <remarks>
/// <para>
/// Both halves live here so a pattern and the ID it has to match cannot drift apart: the
/// pattern is the attribute's constant, the builder is what the command sends, and the <c>*</c>
/// in a pattern is the value Discord.Net hands the handler back as a parameter.
/// </para>
/// <para>
/// An ID carries the row's key rather than the name the editor typed. The name is resolved once,
/// while they are still looking at the command, and the button acts on the row that was named
/// even if it has since been renamed. Discord allows 100 characters, which a prefix and two
/// numbers stay well inside.
/// </para>
/// </remarks>
internal static class FighterAdminIds
{
    public const string AbilityModalPattern = "fighter-admin:ability:*:*";

    public const string RemoveFighterPattern = "fighter-admin:remove-fighter:*";

    public const string RemoveAbilitiesPattern = "fighter-admin:remove-abilities:*";

    public const string Cancel = "fighter-admin:cancel";

    public static string AbilityModal(int variantId, short ordinal) =>
        $"fighter-admin:ability:{Id(variantId)}:{Id(ordinal)}";

    public static string RemoveFighter(int variantId) =>
        $"fighter-admin:remove-fighter:{Id(variantId)}";

    public static string RemoveAbilities(int variantId) =>
        $"fighter-admin:remove-abilities:{Id(variantId)}";

    /// <summary>
    /// Reads a number back out of a custom ID. False when it is not one, which can only happen
    /// if something other than Stanley sent the interaction.
    /// </summary>
    public static bool TryRead(string? value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);

    private static string Id(int value) => value.ToString(CultureInfo.InvariantCulture);
}
