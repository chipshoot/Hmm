using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Automobile
{
    public interface IApplication
    {
        bool Register();

        Subsystem GetApplication();

        NoteCatalog GetCatalog(NoteCatalogType entityType);

        ProcessingResult ProcessingResult { get; }
    }
}