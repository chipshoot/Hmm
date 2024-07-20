// Ignore Spelling: Versioned

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Utility.Dal.DataEntity
{
    public class VersionedEntity : Entity
    {
        [Timestamp]
        [Column("ts")]
        public byte[] Version { get; set; }
    }
}