using System.ComponentModel.DataAnnotations;

namespace Hmm.Core.Map.DomainEntity;

public class EntityBase
{
    public int Id { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }
    
}