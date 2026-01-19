using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutomobile : ApiEntity
    {
        public int Id { get; set; }

        // Core Identification
        public string VIN { get; set; }
        public string Maker { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Trim { get; set; }
        public int Year { get; set; }
        public string Color { get; set; }
        public string Plate { get; set; }

        // Fuel & Engine
        public string EngineType { get; set; }
        public string FuelType { get; set; }
        public decimal FuelTankCapacity { get; set; }
        public decimal CityMPG { get; set; }
        public decimal HighwayMPG { get; set; }
        public decimal CombinedMPG { get; set; }

        // Meter/Odometer
        public long MeterReading { get; set; }
        public int? PurchaseMeterReading { get; set; }

        // Ownership
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public string OwnershipStatus { get; set; }

        // Status
        public bool IsActive { get; set; }
        public DateTime? SoldDate { get; set; }
        public int? SoldMeterReading { get; set; }
        public decimal? SoldPrice { get; set; }

        // Registration & Insurance
        public DateTime? RegistrationExpiryDate { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public string InsuranceProvider { get; set; }
        public string InsurancePolicyNumber { get; set; }

        // Maintenance
        public DateTime? LastServiceDate { get; set; }
        public int? LastServiceMeterReading { get; set; }
        public DateTime? NextServiceDueDate { get; set; }
        public int? NextServiceDueMeterReading { get; set; }

        // Metadata
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAutomobileById", new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAutomobile", new { id }),
                    Rel = "update_automobile",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAutomobile", new { id }),
                    Rel = "patch_automobile",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteAutomobile", new { id }),
                    Rel = "delete_automobile",
                    Method = "DELETE"
                }
            };
        }
    }
}
