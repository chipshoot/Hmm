using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public static class FilterExtensions
    {
        public static void CreateLinks(this ApiAutomobile auto, ActionContext context, LinkGenerator linkGen)
        {
            if (context == null || linkGen == null)
            {
                return;
            }

            var links = new List<Link>
            {
                // self
                new()
                {
                    Title = "self",
                    Rel = "self",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAutomobileById", new { id = auto.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddAuthor",
                    Rel = "create_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddAutomobile", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateAuthor",
                    Rel = "update_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAutomobile", new {id=auto.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchAuthor",
                    Rel = "patch_author",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAutomobile", new {id=auto.Id}),
                    Method = "PATCH"
                }
            };

            auto.Links = links;
        }
    }
}