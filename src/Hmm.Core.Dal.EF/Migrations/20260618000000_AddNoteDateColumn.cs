using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260618000000_AddNoteDateColumn")]
    /// <inheritdoc />
    public partial class AddNoteDateColumn : Migration
    {
        // Hand-written for the same cross-provider drift reason as the
        // earlier hand-written migrations (uuid / attachments). The EF
        // scaffolder re-detects InitialCreate's PG-vs-SqlServer type drift
        // and emits ~30 spurious AlterColumn statements otherwise.
        //
        // Phase 2a: NoteDate is the user-editable note date. CreateDate
        // stays the immutable created-at audit. Existing rows backfill
        // NoteDate from CreateDate so they keep their original date.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.DateTime>(
                name: "notedate",
                table: "notes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql("UPDATE notes SET notedate = createdate;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notedate",
                table: "notes");
        }
    }
}
