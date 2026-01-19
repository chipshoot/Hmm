using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiDiscountForCreate
    {
        [Required]
        [StringLength(100)]
        public string Program { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "CAD";

        [Required]
        public string DiscountType { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string Comment { get; set; }
    }
}
