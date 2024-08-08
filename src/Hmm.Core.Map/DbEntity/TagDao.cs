// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity;

public class TagDao : Entity
{
    [Column("name")]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Column("isactivated")]
    public bool IsActivated { get; set; }

    public ICollection<NoteTagRefDao> Notes { get; set; } = [];
}