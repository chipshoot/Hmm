using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hmm.Core.Dal.EF
{
    public abstract class RepositoryBase
    {
        protected RepositoryBase(IHmmDataContext dataContext, IEntityLookup lookupRepository, IDateTimeProvider dateTimeProvider, ILogger logger = null)
        {
            Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
            Guard.Against<ArgumentNullException>(lookupRepository == null, nameof(lookupRepository));
            Guard.Against<ArgumentNullException>(dateTimeProvider == null, nameof(dateTimeProvider));

            DataContext = dataContext;
            LookupRepository = lookupRepository;
            DateTimeProvider = dateTimeProvider;
            ProcessMessage = logger != null ? new ProcessingResult(logger) : new ProcessingResult();
        }

        protected IEntityLookup LookupRepository { get; }

        protected IDateTimeProvider DateTimeProvider { get; }

        protected IHmmDataContext DataContext { get; }

        protected async Task<TP> PropertyCheckingAsync<TP>(TP property) where TP : HasDefaultEntity
        {
            var defaultNeeded = false;
            if (property == null)
            {
                defaultNeeded = true;
            }
            else if (property.Id <= 0)
            {
                defaultNeeded = true;
            }
            else if (await LookupRepository.GetEntityAsync<TP>(property.Id) == null)
            {
                defaultNeeded = true;
            }

            if (!defaultNeeded)
            {
                return property;
            }

            var defaultProps = await LookupRepository.GetEntitiesAsync<TP>(p => p.IsDefault);
            var defaultProp = defaultProps.FirstOrDefault();
            return defaultProp;
        }

        protected virtual bool HasPropertyChanged<TP>(TP entity, string propertyName)
        {
            return false;
        }

        public ProcessingResult ProcessMessage { get; }
    }
}