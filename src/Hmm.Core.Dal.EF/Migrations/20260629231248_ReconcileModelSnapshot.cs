using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hmm.Core.Dal.EF.Migrations
{
    /// <summary>
    /// Snapshot-reconciliation migration. Carries no DDL.
    ///
    /// The model snapshot had drifted from the model because earlier
    /// hand-written migrations (AddMigrationLogTable, AddAuthorSettingsTable)
    /// did not regenerate <c>HmmDataContextModelSnapshot</c>. That left
    /// <c>authorsettings</c> absent from the snapshot and the model still
    /// mapping a non-existent <c>migrationlogs.description</c> column, which
    /// surfaced as a recurring <c>PendingModelChangesWarning</c> at startup.
    ///
    /// The fix is two parts: (1) ignore the inherited <c>Description</c> on
    /// <c>MigrationLogDao</c> (it was never a real column), and (2) this
    /// migration, whose only job is to regenerate the snapshot so model,
    /// snapshot, and database agree. The <c>authorsettings</c> table already
    /// exists on every deployed database (created by AddAuthorSettingsTable),
    /// so re-creating it here would fail — hence the empty Up/Down.
    /// </summary>
    /// <inheritdoc />
    public partial class ReconcileModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — see class summary. Schema is already
            // current; this migration exists only to advance the snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — nothing was applied in Up.
        }
    }
}
