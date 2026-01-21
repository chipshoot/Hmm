using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiGasStation : ApiEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string ZipCode { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen)
        {
            var id = Id;
            Links =
            [
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetGasStationById", new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateGasStation", new { id }),
                    Rel = "update_station",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "PatchGasStation", new { id }),
                    Rel = "patch_station",
                    Method = "PATCH"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteGasStation", new { id }),
                    Rel = "delete_station",
                    Method = "DELETE"
                }
            ];
        }
    }
}
