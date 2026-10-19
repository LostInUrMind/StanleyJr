using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Stanley.Data.Entities;

namespace Stanley.Data.Conversions;

/// <summary>
/// Maps the change-record enums to the exact strings the database's CHECK constraints allow
/// (database design section 7.1).
/// </summary>
/// <remarks>
/// The mapping is spelled out rather than derived from the enum name, so renaming a member in
/// C# cannot silently change what is written to the column.
/// </remarks>
internal static class ChangeEnumConverters
{
    public static ValueConverter<ChangeAction, string> Action { get; } = new(
        action => ToDatabase(action),
        value => ToAction(value));

    public static ValueConverter<ChangeEntityType, string> EntityType { get; } = new(
        entityType => ToDatabase(entityType),
        value => ToEntityType(value));

    /// <summary>The allowed <c>action</c> values, for the CHECK constraint.</summary>
    public static IReadOnlyList<string> ActionValues { get; } = ["insert", "update", "delete"];

    /// <summary>The allowed <c>entity_type</c> values, for the CHECK constraint.</summary>
    public static IReadOnlyList<string> EntityTypeValues { get; } =
        ["character", "variant", "signature_ability", "variant_alias"];

    private static string ToDatabase(ChangeAction action) => action switch
    {
        ChangeAction.Insert => "insert",
        ChangeAction.Update => "update",
        ChangeAction.Delete => "delete",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    private static ChangeAction ToAction(string value) => value switch
    {
        "insert" => ChangeAction.Insert,
        "update" => ChangeAction.Update,
        "delete" => ChangeAction.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ToDatabase(ChangeEntityType entityType) => entityType switch
    {
        ChangeEntityType.Character => "character",
        ChangeEntityType.Variant => "variant",
        ChangeEntityType.SignatureAbility => "signature_ability",
        ChangeEntityType.VariantAlias => "variant_alias",
        _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null),
    };

    private static ChangeEntityType ToEntityType(string value) => value switch
    {
        "character" => ChangeEntityType.Character,
        "variant" => ChangeEntityType.Variant,
        "signature_ability" => ChangeEntityType.SignatureAbility,
        "variant_alias" => ChangeEntityType.VariantAlias,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
