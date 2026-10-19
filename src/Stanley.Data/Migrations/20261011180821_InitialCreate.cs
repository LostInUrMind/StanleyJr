using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Stanley.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "change_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: true),
                    guild_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_key = table.Column<string>(type: "text", nullable: false),
                    before = table.Column<string>(type: "jsonb", nullable: true),
                    after = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_records", x => x.id);
                    table.CheckConstraint("ck_change_records_action", "action IN ('insert','update','delete')");
                    table.CheckConstraint("ck_change_records_entity_type", "entity_type IN ('character','variant','signature_ability','variant_alias')");
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    archetype = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    atk_ratio = table.Column<short>(type: "smallint", nullable: true),
                    hp_ratio = table.Column<short>(type: "smallint", nullable: true),
                    ability = table.Column<string>(type: "text", nullable: true),
                    ma_name = table.Column<string>(type: "text", nullable: true),
                    ma1_name = table.Column<string>(type: "text", nullable: true),
                    ma1_des = table.Column<string>(type: "text", nullable: true),
                    ma2_name = table.Column<string>(type: "text", nullable: true),
                    ma2_des = table.Column<string>(type: "text", nullable: true),
                    prestige_name = table.Column<string>(type: "text", nullable: true),
                    prestige_des = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_characters", x => x.id);
                    table.CheckConstraint("ck_characters_atk_ratio", "atk_ratio BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_characters_hp_ratio", "hp_ratio BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "elements",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    color_hex = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_elements", x => x.id);
                    table.CheckConstraint("ck_elements_color_hex", "color_hex ~ '^#[0-9A-Fa-f]{6}$'");
                });

            migrationBuilder.CreateTable(
                name: "tiers",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    evolutions_to_max = table.Column<short>(type: "smallint", nullable: false),
                    evo_req = table.Column<short>(type: "smallint", nullable: true),
                    evo_req_special = table.Column<bool>(type: "boolean", nullable: true),
                    evo_special_mat = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiers", x => x.id);
                    table.CheckConstraint("ck_tiers_evolutions_to_max", "evolutions_to_max BETWEEN 0 AND 3");
                });

            migrationBuilder.CreateTable(
                name: "variants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    character_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    quote = table.Column<string>(type: "text", nullable: true),
                    tier_id = table.Column<short>(type: "smallint", nullable: false),
                    element_id = table.Column<short>(type: "smallint", nullable: false),
                    sa_name = table.Column<string>(type: "text", nullable: false),
                    base_atk = table.Column<int>(type: "integer", nullable: false),
                    base_hp = table.Column<int>(type: "integer", nullable: false),
                    base_fs = table.Column<int>(type: "integer", nullable: false),
                    is_raid_boss = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    wiki_url = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variants", x => x.id);
                    table.CheckConstraint("ck_variants_base_stats", "base_atk > 0 AND base_hp > 0 AND base_fs > 0");
                    table.ForeignKey(
                        name: "fk_variants_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_variants_elements_element_id",
                        column: x => x.element_id,
                        principalTable: "elements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_variants_tiers_tier_id",
                        column: x => x.tier_id,
                        principalTable: "tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "signature_abilities",
                columns: table => new
                {
                    variant_id = table.Column<int>(type: "integer", nullable: false),
                    ordinal = table.Column<short>(type: "smallint", nullable: false),
                    ability_text = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_abilities", x => new { x.variant_id, x.ordinal });
                    table.CheckConstraint("ck_signature_abilities_ordinal", "ordinal >= 1");
                    table.CheckConstraint("ck_signature_abilities_text_length", "char_length(ability_text) <= 1024");
                    table.ForeignKey(
                        name: "fk_signature_abilities_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    variant_id = table.Column<int>(type: "integer", nullable: false),
                    alias = table.Column<string>(type: "text", nullable: false),
                    alias_slug = table.Column<string>(type: "text", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variant_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_variant_aliases_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "elements",
                columns: new[] { "id", "color_hex", "name" },
                values: new object[,]
                {
                    { (short)1, "#E74C3C", "Fire" },
                    { (short)2, "#3498DB", "Water" },
                    { (short)3, "#2ECC71", "Air" },
                    { (short)4, "#FFFF82", "Light" },
                    { (short)5, "#9B59B6", "Dark" },
                    { (short)6, "#FF6EC7", "Neutral" }
                });

            migrationBuilder.InsertData(
                table: "tiers",
                columns: new[] { "id", "evo_req", "evo_req_special", "evo_special_mat", "evolutions_to_max", "name" },
                values: new object[,]
                {
                    { (short)1, (short)3, false, null, (short)3, "Bronze" },
                    { (short)2, (short)4, false, null, (short)2, "Silver" },
                    { (short)3, (short)5, true, "Three essences matching the variant's element", (short)1, "Gold" },
                    { (short)4, null, null, null, (short)0, "Diamond" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_changes_entity",
                table: "change_records",
                columns: new[] { "entity_type", "entity_key", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_changes_time",
                table: "change_records",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_characters_name",
                table: "characters",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_elements_name",
                table: "elements",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tiers_evolutions_to_max",
                table: "tiers",
                column: "evolutions_to_max",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tiers_name",
                table: "tiers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aliases_trgm",
                table: "variant_aliases",
                column: "alias")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_aliases_variant",
                table: "variant_aliases",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_variant_aliases_alias_slug",
                table: "variant_aliases",
                column: "alias_slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_character",
                table: "variants",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "ix_variants_element",
                table: "variants",
                column: "element_id");

            migrationBuilder.CreateIndex(
                name: "ix_variants_name",
                table: "variants",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_name_trgm",
                table: "variants",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_variants_slug",
                table: "variants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_variants_tier",
                table: "variants",
                column: "tier_id");

            // The three views of the database design, sections 6.1 and 6.3. EF Core does not
            // generate views, so they are written out here and mapped as keyless entity types.

            // A Raid Boss is not collectible, so it is left out of every count. The condition
            // sits in the join, not in a WHERE, so a tier with no countable variants still
            // returns a row with zero instead of disappearing.
            migrationBuilder.Sql("""
                CREATE VIEW tier_variant_counts AS
                SELECT t.id, t.name, count(v.id) AS total_variants
                FROM tiers t
                LEFT JOIN variants v ON v.tier_id = t.id AND NOT v.is_raid_boss
                GROUP BY t.id, t.name;
                """);

            migrationBuilder.Sql("""
                CREATE VIEW element_variant_counts AS
                SELECT e.id, e.name, count(v.id) AS total_variants
                FROM elements e
                LEFT JOIN variants v ON v.element_id = e.id AND NOT v.is_raid_boss
                GROUP BY e.id, e.name;
                """);

            // Every variant name and every alias in one place, read by the tolerant match
            // (FR-02) and by autocomplete (FR-03). Raid Bosses are included: the exclusion
            // above is about counting, not about lookup.
            migrationBuilder.Sql("""
                CREATE VIEW searchable_names AS
                SELECT v.id AS variant_id, v.name AS label, v.slug AS slug, 'name' AS kind
                FROM variants v
                UNION ALL
                SELECT a.variant_id, a.alias, a.alias_slug, 'alias' AS kind
                FROM variant_aliases a;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Views go first: they depend on the tables below.
            migrationBuilder.Sql("DROP VIEW IF EXISTS searchable_names;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS element_variant_counts;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS tier_variant_counts;");

            migrationBuilder.DropTable(
                name: "change_records");

            migrationBuilder.DropTable(
                name: "signature_abilities");

            migrationBuilder.DropTable(
                name: "variant_aliases");

            migrationBuilder.DropTable(
                name: "variants");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "elements");

            migrationBuilder.DropTable(
                name: "tiers");
        }
    }
}
