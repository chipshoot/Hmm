using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.DtoEntity.Services;

public class PropertyMappingService : IPropertyMappingService
{
    private readonly Dictionary<string, PropertyMappingValue> _gasLogPropertyMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        {"Id", new PropertyMappingValue(new List<string> {"Id"})},
        {"date", new PropertyMappingValue(new List<string> {"CreateDate"})}
    };

    private readonly IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

    public PropertyMappingService()
    {
        _propertyMappings.Add(new PropertyMapping<ApiGasLog, GasLog>(_gasLogPropertyMapping));
    }

    public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
    {
        var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>().ToList();

        if (matchingMapping.Any())
        {
            return matchingMapping.First().MappingDictionary;
        }

        throw new Exception(
            $"Cannot find exact property mapping instance for <{typeof(TSource)}, {typeof(TDestination)}");
    }
}