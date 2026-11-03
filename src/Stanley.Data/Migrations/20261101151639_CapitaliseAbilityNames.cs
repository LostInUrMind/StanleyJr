using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stanley.Data.Migrations
{
    /// <inheritdoc />
    public partial class CapitaliseAbilityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The rows imported from v1 carry whatever casing the wiki used, so 61 of the 315
            // fighters read "Candy Flush" where the card shows "CANDY FLUSH".
            // FighterAdminRepository.Capitalised now stores every SA name in capitals, and this
            // brings the rows already in the table up to that same shape — otherwise the
            // casing a card shows would depend on whether its fighter had been edited since.
            //
            // No change records: those say which editor changed what, and this is a correction
            // to the data model rather than anyone's edit.
            migrationBuilder.Sql(
                """
                UPDATE variants
                SET sa_name = upper(sa_name)
                WHERE sa_name <> upper(sa_name);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty: capitals cannot be undone, because which letters were
            // lower case before is not written down anywhere. Going back a migration leaves
            // the names in capitals, which is valid data either way — every other migration
            // here is reversible, so this is the one to know about.
        }
    }
}
