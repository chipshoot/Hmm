using Hmm.Utility.Dal.DataEntity;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Core.Map.DomainEntity;

public class Tag : Entity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    public bool IsActivated { get; set; }

    public IEnumerable<HmmNote> Notes { get; set; } = [];
}