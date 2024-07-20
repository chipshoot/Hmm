// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity;

public class TagDao : Entity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; }

    [Column("isactivated")]
    public bool IsActivated { get; set; }

    public IEnumerable<NoteTagRefDao> Notes { get; set; }
}