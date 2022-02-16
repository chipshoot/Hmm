using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;

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
                    Title = "AddAutomobile",
                    Rel = "create_automobile",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddAutomobile", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateAutomobile",
                    Rel = "update_automobile",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateAutomobile", new {id=auto.Id}),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchAutomobile",
                    Rel = "patch_automobile",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchAutomobile", new {id=auto.Id}),
                    Method = "PATCH"
                }
            };

            auto.Links = links;
        }

        public static void CreateLinks(this ApiDiscount discount, ActionContext context, LinkGenerator linkGen)
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasDiscountById", new { id = discount.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "AddGasDiscount",
                    Rel = "create_gasDiscount",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddGasDiscount", null),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateGasDiscount",
                    Rel = "update_gasDiscount",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateGasDiscount", new { id = discount.Id }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchGasDiscount",
                    Rel = "patch_gasDiscount",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchGasDiscount", new { id = discount.Id }),
                    Method = "PATCH"
                }
            };

            discount.Links = links;
        }

        public static void CreateLinks(this ApiGasLog log, ActionContext context, LinkGenerator linkGen)
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
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasLogById",
                        new { log.CarId, id = log.Id }),
                    Method = "Get"
                },
                new()
                {
                    Title = "automobile",
                    Rel = "get_automobile",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetAutomobileById", new { id = log.CarId }),
                    Method = "Get"
                },

                new()
                {
                    Title = "AddGasLog",
                    Rel = "create_gasLog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "AddGasLog", new { autoId=log.CarId }),
                    Method = "POST"
                },
                new()
                {
                    Title = "UpdateGasLog",
                    Rel = "update_gasLog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateGasLog",
                        new { autoId=log.CarId, id = log.Id }),
                    Method = "PUT"
                },
                new()
                {
                    Title = "PatchGasLog",
                    Rel = "patch_gasLog",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchGasLog",
                        new { autoId=log.CarId, id = log.Id }),
                    Method = "PATCH"
                }
            };

            if (log.DiscountInfos.Any())
            {
                links.AddRange(log.DiscountInfos.Select(discount => new Link
                {
                    Title = "discount", Rel = "get_discount",
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasDiscountById",
                        new { id = discount.DiscountId }),
                    Method = "Get"
                }));
            }
            log.Links = links;
        }
    }
}