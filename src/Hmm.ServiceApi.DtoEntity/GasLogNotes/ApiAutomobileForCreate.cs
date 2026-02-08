using System;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Data required to create a new automobile.
    /// </summary>
    public class ApiAutomobileForCreate
    {
        [Required]
        [StringLength(17, MinimumLength = 17)]
        public string VIN { get; set; }

        [Required]
        [StringLength(50)]
        public string Maker { get; set; }

        [Required]
        [StringLength(50)]
        public string Brand { get; set; }

        [Required]
        [StringLength(50)]
        public string Model { get; set; }

        [StringLength(30)]
        public string Trim { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [StringLength(30)]
        public string Color { get; set; }

        [Required]
        [StringLength(20)]
        public string Plate { get; set; }

        [Required]
        public string EngineType { get; set; }

        [Required]
        public string FuelType { get; set; }

        [Range(0, 200)]
        public decimal FuelTankCapacity { get; set; }

        [Range(0, 200)]
        public decimal CityMPG { get; set; }

        [Range(0, 200)]
        public decimal HighwayMPG { get; set; }

        [Range(0, 200)]
        public decimal CombinedMPG { get; set; }

        [Range(0, long.MaxValue)]
        public long MeterReading { get; set; }

        public int? PurchaseMeterReading { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PurchasePrice { get; set; }

        public string OwnershipStatus { get; set; } = "Owned";

        public DateTime? RegistrationExpiryDate { get; set; }

        public DateTime? InsuranceExpiryDate { get; set; }

        [StringLength(100)]
        public string InsuranceProvider { get; set; }

        [StringLength(50)]
        public string InsurancePolicyNumber { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
