namespace Stanley.Data.Entities;

/// <summary>
/// One signature ability of one variant (database design section 3.3).
/// </summary>
/// <remarks>
/// <para>
/// One row per ability rather than <c>sa1</c>/<c>sa2</c> columns: a variant has no fixed
/// maximum (the three current Raid Bosses have five, FR-13), every ability must be shown
/// (FR-04), and a card that outgrows Discord's limits is split in SA order (FR-26).
/// </para>
/// <para>
/// A single ability is never removed on its own: removal takes the whole block after a confirmation,
/// and the block is re-entered from SA1. After an edit the repository renumbers what is left,
/// so <see cref="Ordinal"/> always runs from 1 with no gap. Nothing in the database enforces
/// that; the renumbering is what guarantees it.
/// </para>
/// </remarks>
public class SignatureAbility
{
    public int VariantId { get; set; }

    public Variant Variant { get; set; } = null!;

    /// <summary>The SA number: 1 for SA1, upwards with no fixed maximum. Also the display order.</summary>
    public short Ordinal { get; set; }

    /// <summary>
    /// What the ability does. At most <see cref="MaxTextLength"/> characters, checked by the
    /// database. Named <c>AbilityText</c> because <c>text</c> is a type name.
    /// </summary>
    public required string AbilityText { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>The most a Discord embed field displays (FR-13).</summary>
    public const int MaxTextLength = 1024;
}
