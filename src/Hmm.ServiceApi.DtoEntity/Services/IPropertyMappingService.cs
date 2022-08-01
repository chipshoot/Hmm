using Hmm.Utility.Misc;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Services;

public interface IPropertyMappingService
{
    Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();

    bool ValidMappingExistsFor<TSource, TDestination>(string fields);

    ProcessingResult ProcessingResult { get; }
}