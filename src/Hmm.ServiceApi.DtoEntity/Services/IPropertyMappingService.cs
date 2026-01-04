using Hmm.Utility.Misc;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Services;

public interface IPropertyMappingService
{
    ProcessingResult<Dictionary<string, PropertyMappingValue>> GetPropertyMapping<TSource, TDestination>();

    ProcessingResult<bool> ValidMappingExistsFor<TSource, TDestination>(string fields);
}