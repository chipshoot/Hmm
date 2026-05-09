using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutoScheduledService : ApiEntity
    {
        public int Id { get; set; }

        public int AutomobileId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public int? IntervalDays { get; set; }

        public int? IntervalMileage { get; set; }

        public DateTime? NextDueDate { get; set; }

        public int? NextDueMileage { get; set; }

        public bool IsActive { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen, int autoId)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAutoScheduledServiceById", new { autoId, id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAutoScheduledService", new { autoId, id }),
                    Rel = "update_schedule",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAutoScheduledService", new { autoId, id }),
                    Rel = "patch_schedule",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteAutoScheduledService", new { autoId, id }),
                    Rel = "delete_schedule",
                    Method = "DELETE"
                }
            };
        }
    }
}
