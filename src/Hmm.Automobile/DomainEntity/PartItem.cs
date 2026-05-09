using Hmm.Utility.Currency;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    /// <summary>
    /// Single part / labour line on a service record. Nested JSON within ServiceRecord notes.
    /// </summary>
    public class PartItem
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public Money UnitCost { get; set; }
    }
}
