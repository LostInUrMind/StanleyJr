namespace Stanley.Data.Entities;

/// <summary>
/// Which kind of row a change record describes (FR-15). Stored as text with a CHECK
/// constraint, not as a foreign key, so the record of a deletion outlives the row.
/// </summary>
public enum ChangeEntityType
{
    Character = 1,
    Variant = 2,
    SignatureAbility = 3,
    VariantAlias = 4,
}
