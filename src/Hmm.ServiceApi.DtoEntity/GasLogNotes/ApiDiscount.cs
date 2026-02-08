using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Represents a gas discount program in API responses.
    /// </summary>
    public class ApiDiscount : ApiEntity
    {
        public int Id { get; set; }

        public string Program { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string DiscountType { get; set; }

        public bool IsActive { get; set; }

        public string Comment { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen)
        {
            var id = Id;
            Links = new[]
            {
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasDiscountById", new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateGasDiscount", new { id }),
                    Rel = "update_discount",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchGasDiscount", new { id }),
                    Rel = "patch_discount",
                    Method = "PATCH"
                }
            };
        }
    }
}
