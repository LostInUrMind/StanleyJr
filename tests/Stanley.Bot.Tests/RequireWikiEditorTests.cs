using Discord.Interactions;
using Stanley.Bot.Configuration;
using Stanley.Bot.Preconditions;

namespace Stanley.Bot.Tests;

/// <summary>
/// Who may run a write command: the Wiki Editor role, by ID, in the home server, and nobody
/// else (migration plan section 6).
/// </summary>
/// <remarks>
/// The decision is a function of the configuration, the server and the caller's roles, so it
/// is tested as one. Discord itself supplies those three values and has nothing else to say
/// about the answer.
/// </remarks>
public class RequireWikiEditorTests
{
    private const ulong HomeGuild = 111;
    private const ulong OtherGuild = 222;
    private const ulong EditorRole = 333;
    private const ulong SomeOtherRole = 444;

    [Fact]
    public void An_editor_in_the_home_server_is_allowed()
    {
        PreconditionResult result = Check(Configured(), HomeGuild, [SomeOtherRole, EditorRole]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void A_member_of_the_home_server_without_the_role_is_refused()
    {
        PreconditionResult result = Check(Configured(), HomeGuild, [SomeOtherRole]);

        Assert.Equal(RequireWikiEditorAttribute.NotAnEditor, result.ErrorReason);
    }

    [Fact]
    public void The_role_only_counts_in_the_home_server()
    {
        // A role of the same ID in another server, which is all anyone would need to make
        // themselves an editor if the server were not checked too.
        PreconditionResult result = Check(Configured(), OtherGuild, [EditorRole]);

        Assert.Equal(RequireWikiEditorAttribute.NotAnEditor, result.ErrorReason);
    }

    [Fact]
    public void A_caller_outside_any_server_is_refused()
    {
        PreconditionResult result = Check(Configured(), guildId: null, roleIds: null);

        Assert.Equal(RequireWikiEditorAttribute.NotAnEditor, result.ErrorReason);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(HomeGuild, null)]
    [InlineData(null, EditorRole)]
    public void Without_the_role_configured_every_write_is_refused(
        ulong? homeGuildId,
        ulong? editorRoleId)
    {
        // Fails closed. This is the state until FR-17 settles who an editor is, and it must
        // refuse rather than let the first caller through.
        var options = new DiscordOptions
        {
            Token = "token",
            HomeGuildId = homeGuildId,
            WikiEditorRoleId = editorRoleId,
        };

        PreconditionResult result = Check(options, HomeGuild, [EditorRole]);

        Assert.Equal(RequireWikiEditorAttribute.NotConfigured, result.ErrorReason);
    }

    private static DiscordOptions Configured() => new()
    {
        Token = "token",
        HomeGuildId = HomeGuild,
        WikiEditorRoleId = EditorRole,
    };

    private static PreconditionResult Check(
        DiscordOptions options,
        ulong? guildId,
        IReadOnlyCollection<ulong>? roleIds) =>
        RequireWikiEditorAttribute.Check(options, guildId, roleIds);
}
