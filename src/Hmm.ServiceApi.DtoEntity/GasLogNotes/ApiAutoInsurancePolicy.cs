using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutoInsurancePolicy : ApiEntity
    {
        public int Id { get; set; }

        public int AutomobileId { get; set; }

        public string Provider { get; set; }

        public string PolicyNumber { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public decimal Premium { get; set; }

        public string Currency { get; set; }

        public decimal? Deductible { get; set; }

        public List<ApiCoverageItem> Coverage { get; set; } = new();

        public string Notes { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen, int autoId)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAutoInsurancePolicyById", new { autoId, id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAutoInsurancePolicy", new { autoId, id }),
                    Rel = "update_policy",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAutoInsurancePolicy", new { autoId, id }),
                    Rel = "patch_policy",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteAutoInsurancePolicy", new { autoId, id }),
                    Rel = "delete_policy",
                    Method = "DELETE"
                }
            };
        }
    }
}
