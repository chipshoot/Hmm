using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The base class of domain entity who use integer as its identity
    /// </summary>
    public class Entity : AbstractEntity<int>
    {
        [Column("description")]
        [StringLength(1000)]
        public string Description { get; set; }
    }
}