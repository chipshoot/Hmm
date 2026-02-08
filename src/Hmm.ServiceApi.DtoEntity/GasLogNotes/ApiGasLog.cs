using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Represents a gas log entry in API responses.
    /// </summary>
    public class ApiGasLog : ApiEntity
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public int AutomobileId { get; set; }

        // Odometer & Distance
        public decimal Odometer { get; set; }
        public string OdometerUnit { get; set; }
        public decimal Distance { get; set; }
        public string DistanceUnit { get; set; }

        // Fuel Information
        public decimal Fuel { get; set; }
        public string FuelUnit { get; set; }
        public string FuelGrade { get; set; }
        public bool IsFullTank { get; set; }
        public bool IsFirstFillUp { get; set; }

        // Pricing
        public decimal TotalPrice { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; }
        public decimal TotalCostAfterDiscounts { get; set; }

        public List<ApiDiscountInfo> Discounts { get; set; }

        // Station & Location
        public string StationName { get; set; }
        public string Location { get; set; }

        // Driving Context
        public int? CityDrivingPercentage { get; set; }
        public int? HighwayDrivingPercentage { get; set; }
        public string ReceiptNumber { get; set; }

        // Calculated
        public decimal FuelEfficiency { get; set; }

        // Metadata
        public DateTime CreateDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string Comment { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen, int autoId)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasLogById", new { autoId, id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateGasLog", new { autoId, id }),
                    Rel = "update_gaslog",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchGasLog", new { autoId, id }),
                    Rel = "patch_gaslog",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteGasLog", new { autoId, id }),
                    Rel = "delete_gaslog",
                    Method = "DELETE"
                }
            };
        }
    }
}
