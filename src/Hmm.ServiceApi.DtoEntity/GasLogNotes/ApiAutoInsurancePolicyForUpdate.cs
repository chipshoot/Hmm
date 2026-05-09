using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutoInsurancePolicyForUpdate
    {
        [StringLength(100)]
        public string Provider { get; set; }

        [StringLength(50)]
        public string PolicyNumber { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Premium { get; set; }

        public string Currency { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Deductible { get; set; }

        public List<ApiCoverageItem> Coverage { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        public bool? IsActive { get; set; }
    }
}
