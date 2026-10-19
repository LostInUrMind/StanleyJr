using Discord;
using Stanley.Data.Changes;

namespace Stanley.Bot.Modules;

/// <summary>
/// Who ran a write command and where, which is the half of a change record that is not the
/// data itself (FR-15).
/// </summary>
/// <remarks>
/// Shared by every module that writes, so the attribution of a change cannot depend on which
/// command made it.
/// </remarks>
internal static class EditorContexts
{
    /// <summary>
    /// The editor behind <paramref name="context"/>. A snowflake's top bit is clear, so the
    /// cast to the signed <c>bigint</c> the column holds cannot overflow.
    /// </summary>
    public static EditorContext Of(IInteractionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new EditorContext(
            (long)context.User.Id,
            context.Interaction?.GuildId is ulong guildId ? (long)guildId : null);
    }
}
