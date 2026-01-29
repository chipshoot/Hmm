// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity;

public class ContactDao : Entity, IActivatable
{
    [Column("contact")]
    public string Contact { get; set; }

    [Column("isactivated")]
    public bool IsActivated { get; set; }
}