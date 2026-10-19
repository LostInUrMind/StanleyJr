using System.Text.Json;
using Stanley.Data.Entities;

namespace Stanley.Data.Changes;

/// <summary>
/// Builds the one <see cref="ChangeRecord"/> an editor write appends.
/// </summary>
/// <remarks>
/// Every write command goes through this, so <c>before</c> and <c>after</c> have the same JSON
/// shape whichever command wrote them and an undo (FR-16) has one shape to read.
/// <see cref="ChangeRecord.OccurredAt"/> is left unset: the column defaults to
/// <c>now()</c>, so the time comes from the database that is also committing the change.
/// </remarks>
public static class ChangeRecords
{
    /// <summary>
    /// camelCase and cached: the snapshot reads like the entity it was taken from, and
    /// building the options once per call is the documented way to lose the serializer's
    /// metadata cache.
    /// </summary>
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web);

    /// <summary>Records a row that did not exist before.</summary>
    public static ChangeRecord Insert<TSnapshot>(
        EditorContext editor,
        ChangeEntityType entityType,
        string entityKey,
        TSnapshot after) =>
        Build(editor, ChangeAction.Insert, entityType, entityKey, null, Serialize(after));

    /// <summary>Records a row that changed, with both of its states.</summary>
    public static ChangeRecord Update<TSnapshot>(
        EditorContext editor,
        ChangeEntityType entityType,
        string entityKey,
        TSnapshot before,
        TSnapshot after) =>
        Build(
            editor,
            ChangeAction.Update,
            entityType,
            entityKey,
            Serialize(before),
            Serialize(after));

    public static ChangeRecord Delete<TSnapshot>(
        EditorContext editor,
        ChangeEntityType entityType,
        string entityKey,
        TSnapshot before) =>
        Build(editor, ChangeAction.Delete, entityType, entityKey, Serialize(before), null);

    private static ChangeRecord Build(
        EditorContext editor,
        ChangeAction action,
        ChangeEntityType entityType,
        string entityKey,
        string? before,
        string? after) =>
        new()
        {
            ActorUserId = editor.ActorUserId,
            GuildId = editor.GuildId,
            Action = action,
            EntityType = entityType,
            EntityKey = entityKey,
            Before = before,
            After = after,
        };

    private static string Serialize<TSnapshot>(TSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, SnapshotJson);
}
