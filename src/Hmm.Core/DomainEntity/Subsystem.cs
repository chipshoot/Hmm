using System.Collections.Generic;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity
{
    public class Subsystem : HasDefaultEntity
    {
        public string Name { get; set; }

        public IEnumerable<NoteCatalog> NoteCatalogs { get; set; }
    }
}