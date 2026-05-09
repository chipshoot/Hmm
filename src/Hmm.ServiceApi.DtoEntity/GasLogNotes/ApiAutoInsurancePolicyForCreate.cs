using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutoInsurancePolicyForCreate
    {
        [Required]
        [StringLength(100)]
        public string Provider { get; set; }

        [Required]
        [StringLength(50)]
        public string PolicyNumber { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Premium { get; set; }

        public string Currency { get; set; } = "CAD";

        [Range(0, double.MaxValue)]
        public decimal? Deductible { get; set; }

        public List<ApiCoverageItem> Coverage { get; set; } = new();

        [StringLength(1000)]
        public string Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
