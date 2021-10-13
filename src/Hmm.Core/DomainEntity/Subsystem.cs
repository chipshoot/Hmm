using System.Collections.Generic;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity
{
    /// <summary>
    /// The class is used to hold system fundamental information and help application to
    /// register, update its information. e.g. Automobile information manager system can
    /// have default note author and a set of note catalogs, e.g. GasLog, GasDiscount,
    /// AutomobileInfo etc.
    /// </summary>
    public class Subsystem : HasDefaultEntity
    {
        public string Name { get; set; }

        public Author DefaultAuthor { get; set; }

        public IEnumerable<NoteCatalog> NoteCatalogs { get; set; }
    }
}