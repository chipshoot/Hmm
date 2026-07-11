using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiServiceRecordForCreate
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Mileage { get; set; }

        // Legacy primary category, still accepted for one release. `Types` is
        // authoritative; when both are absent the mapper defaults to Other.
        public string Type { get; set; }

        public List<string> Types { get; set; } = new();

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public decimal? Tax { get; set; }

        public string Currency { get; set; } = "CAD";

        [StringLength(100)]
        public string ShopName { get; set; }

        public List<ApiPartItem> Parts { get; set; } = new();

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
