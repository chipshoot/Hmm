using System;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiGasLogForUpdate
    {
        public DateTime? Date { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Odometer { get; set; }

        public string OdometerUnit { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Distance { get; set; }

        public string DistanceUnit { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Fuel { get; set; }

        public string FuelUnit { get; set; }

        public string FuelGrade { get; set; }

        public bool? IsFullTank { get; set; }

        public bool? IsFirstFillUp { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? TotalPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitPrice { get; set; }

        public string Currency { get; set; }

        public int? StationId { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

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
