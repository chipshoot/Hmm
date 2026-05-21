using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DomainEntity
{
    /// <summary>
    /// Domain projection of <see cref="MigrationLogDao"/>. One row
    /// per bulk migration operation (upload / export / replace /
    /// lapsed delete) — see <c>docs/multi-device-cloud-sync.md</c>.
    /// </summary>
    public class MigrationLog : Entity
    {
        public int AuthorId { get; set; }

        /// <summary>
        /// Client-supplied device identifier (e.g. install UUID).
        /// Free-form string until the <c>Devices</c> entity lands.
        /// </summary>
        public string? DeviceIdentifier { get; set; }

        public MigrationLogKind Kind { get; set; }

        /// <summary>
        /// Counts JSON — see <see cref="MigrationLogDao.RecordCounts"/>.
        /// </summary>
        public string? RecordCounts { get; set; }

        public System.DateTime At { get; set; }
    }
}
