using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiServiceRecord : ApiEntity
    {
        public int Id { get; set; }

        public int AutomobileId { get; set; }

        public DateTime Date { get; set; }

        public int Mileage { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }

        public decimal? Cost { get; set; }

        public string Currency { get; set; }

        public string ShopName { get; set; }

        public List<ApiPartItem> Parts { get; set; } = new();

        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen, int autoId)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetServiceRecordById", new { autoId, id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateServiceRecord", new { autoId, id }),
                    Rel = "update_service",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchServiceRecord", new { autoId, id }),
                    Rel = "patch_service",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteServiceRecord", new { autoId, id }),
                    Rel = "delete_service",
                    Method = "DELETE"
                }
            };
        }
    }
}
