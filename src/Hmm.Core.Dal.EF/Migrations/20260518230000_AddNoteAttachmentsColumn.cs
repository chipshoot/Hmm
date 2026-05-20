using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    // Both [DbContext] and [Migration] are required for EF Core to
    // associate this migration with HmmDataContext and recognise its
    // id. The scaffolder emits them on the .Designer.cs companion
    // (paired with the model snapshot at that point), but since we
    // hand-wrote this migration to dodge the cross-provider drift in
    // InitialCreate, both attributes live here directly. Missing
    // either makes Migrate() silently skip the file — see EF Core
    // discovery in MigrationsAssembly.GetMigrationsAttributes.
    [DbContext(typeof(HmmDataContext))]
    [Migration("20260518230000_AddNoteAttachmentsColumn")]
    /// <inheritdoc />
    public partial class AddNoteAttachmentsColumn : Migration
    {
        // Hand-written rather than scaffolded via `dotnet ef
        // migrations add` because `InitialCreate` was generated
        // against PostgreSQL while the dev's default connection is
        // SQL Server — the EF tool re-detected the type drift on
        // every column and emitted ~30 spurious AlterColumn
        // statements alongside the one column we actually wanted.
        // Adding just the column we need; the provider-type drift
        // is a pre-existing issue tracked separately and not for
        // this phase to fix.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attachments",
                table: "notes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attachments",
                table: "notes");
        }
    }
}
