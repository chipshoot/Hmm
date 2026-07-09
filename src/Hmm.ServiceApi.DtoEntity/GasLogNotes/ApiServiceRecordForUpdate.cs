using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiServiceRecordForUpdate
    {
        public DateTime? Date { get; set; }

        [Range(0, int.MaxValue)]
        public int? Mileage { get; set; }

        public string Type { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public decimal? Tax { get; set; }

        public string Currency { get; set; }

        [StringLength(100)]
        public string ShopName { get; set; }

        public List<ApiPartItem> Parts { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
