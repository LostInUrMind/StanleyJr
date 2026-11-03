using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Stanley.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_change_records_entity_type",
                table: "change_records");

            migrationBuilder.CreateTable(
                name: "wiki_pages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    key_slug = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wiki_pages", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_change_records_entity_type",
                table: "change_records",
                sql: "entity_type IN ('character','variant','signature_ability','variant_alias','wiki_page')");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_key_slug",
                table: "wiki_pages",
                column: "key_slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_trgm",
                table: "wiki_pages",
                column: "key")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            // The two pages that were a hardcoded list in Stanley.Core until this table
            // existed, under every name v1 answered to, so no /wiki input changes its answer.
            // Written as data rather than as model seed data: editors own these rows from now
            // on, and HasData would put back whatever one of them removed. The slugs are what
            // NameNormalizer.Normalize returns for the keys.
            migrationBuilder.InsertData(
                table: "wiki_pages",
                columns: new[] { "key", "key_slug", "url" },
                values: new object[,]
                {
                    {
                        "Critless Skill Tree",
                        "critlessskilltree",
                        "https://skullgirlsmobile.fandom.com/wiki/Critless_Skill_Tree"
                    },
                    {
                        "critless",
                        "critless",
                        "https://skullgirlsmobile.fandom.com/wiki/Critless_Skill_Tree"
                    },
                    {
                        "Fortune's Favor",
                        "fortunesfavor",
                        "https://skullgirlsmobile.fandom.com/wiki/Fortune's_Favor"
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wiki_pages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_change_records_entity_type",
                table: "change_records");

            migrationBuilder.AddCheckConstraint(
                name: "ck_change_records_entity_type",
                table: "change_records",
                sql: "entity_type IN ('character','variant','signature_ability','variant_alias')");
        }
    }
}
