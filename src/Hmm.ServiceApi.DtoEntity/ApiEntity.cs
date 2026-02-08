using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity
{
    /// <summary>
    /// Base class for all API DTOs, providing HATEOAS link support.
    /// </summary>
    public abstract class ApiEntity
    {
        protected ApiEntity()
        {
            Links = new List<Link>();
        }

        public IEnumerable<Link> Links { get; set; }
    }
}