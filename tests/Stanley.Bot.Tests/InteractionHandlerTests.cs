using Stanley.Bot.Hosting;

namespace Stanley.Bot.Tests;

/// <summary>
/// How a failed interaction is reported. Reporting a failure must not fail: an exception
/// raised here happens inside Discord.Net's event rather than in a command, so it is logged as
/// an unhandled dispatch failure and the user is left with no reply at all.
/// </summary>
public class InteractionHandlerTests
{
    [Fact]
    public void An_interaction_that_matched_no_command_still_has_something_to_log()
    {
        // Discord.Net reports a command it could not find with a null command info. Running a
        // command Discord still offers but this build has dropped — a stale registration, as
        // /alias add became /alias-admin add — is exactly how that happens, and reading the
        // name off it threw a NullReferenceException.
        Assert.False(string.IsNullOrWhiteSpace(InteractionHandler.Describe(null)));
    }
}
