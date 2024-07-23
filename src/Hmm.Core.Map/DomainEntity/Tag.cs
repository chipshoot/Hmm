using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Map.DomainEntity;

public class Tag : EntityBase
{
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; }

    [Column("isactivated")]
    public bool IsActivated { get; set; }

    public IEnumerable<NoteTagRef> Notes { get; set; }
}