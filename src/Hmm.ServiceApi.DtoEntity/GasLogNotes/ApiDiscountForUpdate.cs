using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Data required to update an existing gas discount program.
    /// </summary>
    public class ApiDiscountForUpdate
    {
        [StringLength(100)]
        public string Program { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Amount { get; set; }

        public string Currency { get; set; }

        public string DiscountType { get; set; }

        public bool? IsActive { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }
}
