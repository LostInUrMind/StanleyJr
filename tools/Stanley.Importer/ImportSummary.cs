namespace Stanley.Importer;

internal sealed class ImportSummary
{
    public int FightersInFile { get; set; }

    public int CharactersCreated { get; set; }

    public int VariantsCreated { get; set; }

    public int AbilitiesCreated { get; set; }

    public int AbilitiesRemoved { get; set; }

    /// <summary>Rows the save actually wrote. Zero on a repeat run.</summary>
    public int RowsWritten { get; set; }
}
