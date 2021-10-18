using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity
{
    public abstract class ApiEntity
    {
        protected ApiEntity()
        {
            Links = new List<Link>();
        }

        public List<Link> Links { get; }
    }
}