// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity
{
    public class AuthorDao : Entity
    {
        [Column("accountname")]
        [StringLength(256)]
        public string AccountName { get; set; }

        [ForeignKey("contactinfo")]
        public ContactDao ContactInfo { get; set; }

        [Column("role")]
        public AuthorRoleType Role { get; set; }

        [Column("isactivated")]
        public bool IsActivated { get; set; }
    }
}