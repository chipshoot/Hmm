using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;

namespace Hmm.Core.Dal.EF
{
    public abstract class RepositoryBase
    {
        protected RepositoryBase(IHmmDataContext dataContext, IEntityLookup lookupRepository, IDateTimeProvider dateTimeProvider)
        {
            Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
            Guard.Against<ArgumentNullException>(lookupRepository == null, nameof(lookupRepository));
            Guard.Against<ArgumentNullException>(dateTimeProvider == null, nameof(dateTimeProvider));

            DataContext = dataContext;
            LookupRepository = lookupRepository;
            DateTimeProvider = dateTimeProvider;
        }

        protected IEntityLookup LookupRepository { get; }

        protected IDateTimeProvider DateTimeProvider { get; }

        protected IHmmDataContext DataContext { get; }

        protected TP PropertyChecking<TP>(TP property) where TP : HasDefaultEntity
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
            else if (LookupRepository.GetEntity<TP>(property.Id) == null)
            {
                defaultNeeded = true;
            }

            if (!defaultNeeded)
            {
                return property;
            }

            var defaultProp = LookupRepository.GetEntities<TP>(p => p.IsDefault).FirstOrDefault();
            return defaultProp;
        }

        protected virtual bool HasPropertyChanged<TP>(TP entity, string propertyName)
        {
            return false;
        }

        public ProcessingResult ProcessMessage { get; } = new();
    }
}