using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.Core.Map.DbEntity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

                if (typeof(TP) == typeof(NoteCatalogDao))
                {
                    var defaultCatalog = await DataContext.Catalogs.Cast<TP>().FirstOrDefaultAsync(c => c.IsDefault);
                    if (defaultCatalog == null)
                    {
                        return ProcessingResult<TP>.NotFound($"No default {typeof(TP).Name} found");
                    }
                    return ProcessingResult<TP>.Ok(defaultCatalog);
                }

                return ProcessingResult<TP>.Fail($"{typeof(TP)} is not supported");
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