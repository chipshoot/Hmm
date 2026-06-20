using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260619000000_AddNoteLocationColumns")]
    /// <inheritdoc />
    public partial class AddNoteLocationColumns : Migration
    {
        // Hand-written for the same cross-provider drift reason as the
        // earlier migrations. Phase 2b: optional note location, three
        // nullable columns, no backfill (existing notes have no location).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "latitude", table: "notes",
                type: "double precision", nullable: true);
            migrationBuilder.AddColumn<double>(
                name: "longitude", table: "notes",
                type: "double precision", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "locationlabel", table: "notes",
                type: "character varying(500)", maxLength: 500, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "latitude", table: "notes");
            migrationBuilder.DropColumn(name: "longitude", table: "notes");
            migrationBuilder.DropColumn(name: "locationlabel", table: "notes");
        }
    }
}
