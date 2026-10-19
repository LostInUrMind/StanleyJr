namespace Stanley.Data.Entities;

/// <summary>
/// One row per editor write: who, when, what, and the values before and after (FR-15).
/// Database design section 3.7.
/// </summary>
/// <remarks>
/// <para>
/// This is the only table holding anything about a person, and only about editors. There is no
/// editors table: who may edit is a Discord role checked by ID at run time (FR-17).
/// </para>
/// <para>
/// A write command appends exactly one row in the same transaction as the change: if the
/// record cannot be written, the change is not committed. Nothing is ever deleted from this
/// table by the bot, and an undo (FR-16) is applied as a new change that is itself recorded.
/// </para>
/// </remarks>
public class ChangeRecord
{
    /// <summary><c>bigint</c>: this is the one table that grows without limit.</summary>
    public long Id { get; set; }

    /// <summary>
    /// Discord ID of the editor. Nullable so it can be cleared within 30 days of a removal
    /// request while the change itself is kept (CR-02).
    /// </summary>
    public long? ActorUserId { get; set; }

    /// <summary>Server the command was run in.</summary>
    public long? GuildId { get; set; }

    public ChangeAction Action { get; set; }

    public ChangeEntityType EntityType { get; set; }

    /// <summary>
    /// Which row, as text: an id, or <c>42:3</c> for a signature ability. Text because the key
    /// is composite for one entity and the row may no longer exist.
    /// </summary>
    public required string EntityKey { get; set; }

    /// <summary>The row before the change, as JSON. Null for an insert. What an undo reads.</summary>
    public string? Before { get; set; }

    /// <summary>The row after the change, as JSON. Null for a delete.</summary>
    public string? After { get; set; }

    public DateTime OccurredAt { get; set; }
}
