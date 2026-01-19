using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiGasLogForCreation
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int AutomobileId { get; set; }

        // Odometer & Distance
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Odometer { get; set; }

        public string OdometerUnit { get; set; } = "Mile";

        [Range(0, double.MaxValue)]
        public decimal Distance { get; set; }

        public string DistanceUnit { get; set; } = "Mile";

        // Fuel Information
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Fuel { get; set; }

        public string FuelUnit { get; set; } = "Gallon";

        [Required]
        public string FuelGrade { get; set; }

        public bool IsFullTank { get; set; } = true;

        public bool IsFirstFillUp { get; set; }

        // Pricing
        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string Currency { get; set; } = "CAD";

        public List<ApiDiscountInfo> DiscountInfos { get; set; }

        // Station & Location
        public int? StationId { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        // Driving Context
        [Range(0, 100)]
        public int? CityDrivingPercentage { get; set; }

        [Range(0, 100)]
        public int? HighwayDrivingPercentage { get; set; }

        [StringLength(50)]
        public string ReceiptNumber { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }
    }
}
