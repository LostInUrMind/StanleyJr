using System.Reflection;
using Discord.Interactions;
using Stanley.Bot.Modules;
using Stanley.Bot.Preconditions;

namespace Stanley.Bot.Tests;

/// <summary>
/// Where the editor commands are registered. A write command belongs to a top-level command of
/// its own, marked <c>[DontAutoRegister]</c>, which is what lets <c>DiscordBotService</c>
/// register it to the home server alone (migration plan section 6).
/// </summary>
/// <remarks>
/// Checked by reflection because the mistake is invisible otherwise: a write subcommand added
/// beside a read command compiles, passes every other test, and is then registered globally —
/// in every server — with nothing but the role check in front of it. That is what
/// <c>/alias add</c> was before it became <c>/alias-admin add</c>.
/// </remarks>
public class EditorCommandRegistrationTests
{
    [Fact]
    public void A_module_with_an_editor_command_is_registered_to_the_home_server_alone()
    {
        string[] offenders =
        [
            .. Modules()
                .Where(module => SlashCommands(module).Any(IsEditorOnly))
                .Where(module => !Has<DontAutoRegisterAttribute>(module))
                .Select(module => module.Name)
                .Order(StringComparer.Ordinal),
        ];

        Assert.Empty(offenders);
    }

    [Fact]
    public void A_module_registered_to_the_home_server_alone_only_holds_editor_commands()
    {
        // The other direction: a command kept out of the global registration appears in the
        // home server only, which would quietly hide a read command from everyone else.
        string[] offenders =
        [
            .. Modules()
                .Where(Has<DontAutoRegisterAttribute>)
                .Where(module => SlashCommands(module).Any(command => !IsEditorOnly(command)))
                .Select(module => module.Name)
                .Order(StringComparer.Ordinal),
        ];

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_editor_commands_are_the_two_admin_commands()
    {
        // Named rather than counted, so adding a third is a decision and not a surprise.
        string[] groups =
        [
            .. Modules()
                .Where(Has<DontAutoRegisterAttribute>)
                .Select(module => module.GetCustomAttribute<GroupAttribute>()?.Name ?? module.Name)
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(["alias-admin", "fighter-admin"], groups);
    }

    /// <summary>Every command module in the bot.</summary>
    private static IEnumerable<Type> Modules() =>
        typeof(AliasModule).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract && typeof(IInteractionModuleBase).IsAssignableFrom(type));

    private static IEnumerable<MethodInfo> SlashCommands(Type module) =>
        module
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<SlashCommandAttribute>() is not null);

    /// <summary>
    /// A command only Wiki Editors may run, whether the precondition is on the method or on the
    /// module around it.
    /// </summary>
    private static bool IsEditorOnly(MethodInfo command) =>
        command.GetCustomAttribute<RequireWikiEditorAttribute>() is not null
        || Has<RequireWikiEditorAttribute>(command.DeclaringType!);

    private static bool Has<TAttribute>(Type module)
        where TAttribute : Attribute => module.GetCustomAttribute<TAttribute>() is not null;
}
