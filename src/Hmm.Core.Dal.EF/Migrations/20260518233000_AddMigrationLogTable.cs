using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260518233000_AddMigrationLogTable")]
    /// <inheritdoc />
    public partial class AddMigrationLogTable : Migration
    {
        // Hand-written for the same reason as the Phase 6b
        // AddNoteAttachmentsColumn migration: the existing
        // InitialCreate was scaffolded against PostgreSQL while the
        // dev's default connection is SQL Server, so `dotnet ef
        // migrations add` re-emits ~30 spurious AlterColumn entries
        // alongside the real change. Keeping this file focused on
        // the actual delta until the provider-drift issue is sorted
        // separately.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "migrationlogs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            Npgsql.EntityFrameworkCore.PostgreSQL.Metadata
                                .NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    authorid = table.Column<int>(type: "integer", nullable: false),
                    device = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80, nullable: true),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    recordcounts = table.Column<string>(type: "text", nullable: true),
                    at = table.Column<System.DateTime>(
                        type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migrationlogs", x => x.id);
                    table.ForeignKey(
                        name: "fk_migrationlogs_authors",
                        column: x => x.authorid,
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "ix_migrationlogs_authorid",
                table: "migrationlogs",
                column: "authorid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "migrationlogs");
        }
    }
}
