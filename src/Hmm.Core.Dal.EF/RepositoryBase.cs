using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    public abstract class RepositoryBase
    {
        protected RepositoryBase(IHmmDataContext dataContext, IEntityLookup lookupRepository, IDateTimeProvider dateTimeProvider, ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepository);
            ArgumentNullException.ThrowIfNull(dateTimeProvider);

            DataContext = dataContext;
            LookupRepository = lookupRepository;
            DateTimeProvider = dateTimeProvider;
            Logger = logger;
        }

        protected IEntityLookup LookupRepository { get; }

        protected IDateTimeProvider DateTimeProvider { get; }

        protected IHmmDataContext DataContext { get; }

        protected ILogger Logger { get; }

        /// <summary>
        /// Validates a property and returns the default entity if the provided one is invalid.
        /// This method follows the Open/Closed Principle - it works with any entity type that
        /// extends HasDefaultEntity without requiring modification for new types.
        /// </summary>
        /// <typeparam name="TP">Entity type that extends HasDefaultEntity</typeparam>
        /// <param name="property">The property to validate</param>
        /// <returns>The validated property or the default entity</returns>
        protected async Task<ProcessingResult<TP>> PropertyCheckingAsync<TP>(TP property) where TP : HasDefaultEntity
        {
            try
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
                else
                {
                    var lookupResult = await LookupRepository.GetEntityAsync<TP>(property.Id);
                    if (!lookupResult.Success)
                    {
                        defaultNeeded = true;
                    }
                }

                if (!defaultNeeded)
                {
                    return ProcessingResult<TP>.Ok(property);
                }

                // Use the generic GetDefaultEntityAsync method - no hardcoded type checks needed
                var defaultEntity = await DataContext.GetDefaultEntityAsync<TP>();
                if (defaultEntity == null)
                {
                    return ProcessingResult<TP>.NotFound($"No default {typeof(TP).Name} found");
                }

                return ProcessingResult<TP>.Ok(defaultEntity);
            }
            catch (Exception ex)
            {
                return ProcessingResult<TP>.FromException(ex);
            }
        }

        protected virtual bool HasPropertyChanged<TP>(TP entity, string propertyName)
        {
            return false;
        }
    }
}