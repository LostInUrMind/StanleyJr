namespace Stanley.Data.Entities;

/// <summary>What an editor did to a row (FR-15). Stored as text with a CHECK constraint.</summary>
public enum ChangeAction
{
    Insert = 1,
    Update = 2,
    Delete = 3,
}
