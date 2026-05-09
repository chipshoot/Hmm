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

        [Required]
        public string Type { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public string Currency { get; set; } = "CAD";

        [StringLength(100)]
        public string ShopName { get; set; }

        public List<ApiPartItem> Parts { get; set; } = new();

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
