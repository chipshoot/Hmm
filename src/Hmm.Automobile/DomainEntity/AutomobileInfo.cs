using System;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    public class AutomobileInfo : AutomobileBase
    {
        // ===== Core Identification =====
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

        // ===== Fuel & Engine =====
        [Required]
        public FuelEngineType EngineType { get; set; }

        [Required]
        public FuelGrade FuelType { get; set; }

        [Range(0, 200)]
        public decimal FuelTankCapacity { get; set; }

        [Range(0, 200)]
        public decimal CityMPG { get; set; }

        [Range(0, 200)]
        public decimal HighwayMPG { get; set; }

        [Range(0, 200)]
        public decimal CombinedMPG { get; set; }

        // ===== Meter/Odometer =====
        [Range(0, long.MaxValue)]
        public long MeterReading { get; set; }

        public int? PurchaseMeterReading { get; set; }

        // ===== Ownership =====
        public DateTime? PurchaseDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PurchasePrice { get; set; }

        public OwnershipType OwnershipStatus { get; set; } = OwnershipType.Owned;

        // ===== Status =====
        public bool IsActive { get; set; } = true;

        public DateTime? SoldDate { get; set; }

        public int? SoldMeterReading { get; set; }

        public decimal? SoldPrice { get; set; }

        // ===== Registration & Insurance =====
        public DateTime? RegistrationExpiryDate { get; set; }

        public DateTime? InsuranceExpiryDate { get; set; }

        [StringLength(100)]
        public string InsuranceProvider { get; set; }

        [StringLength(50)]
        public string InsurancePolicyNumber { get; set; }

        // ===== Maintenance =====
        public DateTime? LastServiceDate { get; set; }

        public int? LastServiceMeterReading { get; set; }

        public DateTime? NextServiceDueDate { get; set; }

        public int? NextServiceDueMeterReading { get; set; }

        // ===== Metadata =====
        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }

    // ===== Supporting Enums =====
    public enum FuelEngineType
    {
        Gasoline,
        Diesel,
        Hybrid,
        PlugInHybrid,
        Electric,
        Hydrogen,
        CNG // Compressed Natural Gas
    }

    public enum FuelGrade
    {
        Regular,
        MidGrade,
        Premium,
        Diesel,
        E85,
        Electric,
        Other
    }

    public enum OwnershipType
    {
        Owned,
        Financed,
        Leased,
        Company
    }
}