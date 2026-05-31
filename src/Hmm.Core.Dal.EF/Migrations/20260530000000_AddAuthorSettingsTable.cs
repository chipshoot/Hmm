using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260530000000_AddAuthorSettingsTable")]
    /// <inheritdoc />
    public partial class AddAuthorSettingsTable : Migration
    {
        // Hand-written for the same reason as the AddMigrationLogTable /
        // AddNoteAttachmentsColumn migrations: `dotnet ef migrations
        // add` re-emits ~30 spurious AlterColumn entries due to the
        // PostgreSQL-vs-SQL-Server provider drift in InitialCreate.
        // Keeping this focused on the real delta. PG is the deployed
        // target (Migrate()); SQLite spins the same table from the
        // model via EnsureCreated(). Includes the `description` column
        // because AuthorSettingsDao derives from Entity, so the EF
        // model expects it — omitting it (as AddMigrationLogTable did)
        // would drift the PG schema from the model.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authorsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
                                .NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authorid = table.Column<int>(type: "integer", nullable: false),
                    settingsjson = table.Column<string>(type: "text", nullable: true),
                    lastmodified = table.Column<System.DateTime>(
                        type: "timestamp with time zone", nullable: false),
                    updatedat = table.Column<System.DateTime>(
                        type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorsettings", x => x.id);
                    table.ForeignKey(
                        name: "fk_authorsettings_authors",
                        column: x => x.authorid,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // One settings row per author.
            migrationBuilder.CreateIndex(
                name: "uq_authorsettings_authorid",
                table: "authorsettings",
                column: "authorid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "authorsettings");
        }
    }
}
