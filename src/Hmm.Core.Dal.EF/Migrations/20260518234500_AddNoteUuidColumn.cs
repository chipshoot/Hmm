using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260518234500_AddNoteUuidColumn")]
    /// <inheritdoc />
    public partial class AddNoteUuidColumn : Migration
    {
        // Hand-written for the same provider-drift reason as the
        // earlier Phase 6b / Phase 7 migrations.
        //
        // Phase 15b: gives HmmNote a cross-device-stable identity
        // (separate from Id, which stays the internal FK target).
        // Nullable so existing rows survive the migration; the
        // manager assigns a value on the next Create/Update.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uuid",
                table: "notes",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            // PG + SQLite treat multiple NULLs as distinct under a
            // UNIQUE index, so we don't need a partial / filtered
            // index. SQL Server would; if/when the API targets
            // that provider we'll switch to a filtered index here.
            migrationBuilder.CreateIndex(
                name: "uq_notes_uuid",
                table: "notes",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_notes_uuid",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "uuid",
                table: "notes");
        }
    }
}
