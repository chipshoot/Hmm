// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Dal.EF.DbEntity;

public class ContactDao : Entity
{
    [Column("contact")]
    public string Contact { get; set; }

    [Column("isactivated")]
    public bool IsActivated { get; set; }
}