using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    /// <summary>
    /// Single coverage line on an auto insurance policy (e.g. Liability, Collision, Comprehensive).
    /// Stored as nested JSON inside the policy's note content; not an independent persistence type.
    /// </summary>
    public class CoverageItem
    {
        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Limit { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Deductible { get; set; }

        [StringLength(10)]
        public string Currency { get; set; }
    }
}
