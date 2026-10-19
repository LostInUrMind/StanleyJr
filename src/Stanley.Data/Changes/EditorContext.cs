namespace Stanley.Data.Changes;

/// <summary>
/// Who ran a write command and where, which is the half of a change record that is not the
/// data itself (FR-15).
/// </summary>
/// <param name="ActorUserId">
/// The editor's Discord ID. Nullable because it is cleared on request while the change itself
/// is kept, so a record that has been through that still has to be representable.
/// </param>
/// <param name="GuildId">The server the command was run in.</param>
public readonly record struct EditorContext(long? ActorUserId, long? GuildId);
