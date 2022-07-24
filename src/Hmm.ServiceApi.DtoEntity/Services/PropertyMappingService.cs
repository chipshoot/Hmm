using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.Utility.Misc;
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

    public ProcessingResult ProcessingResult { get; set; } = new ProcessingResult();

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

    public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
    {
        if (string.IsNullOrEmpty(fields))
        {
            return true;
        }

        var propertyMapping = GetPropertyMapping<TSource, TDestination>();
        var fieldArr = fields.Split(',');
        foreach (var fieldRaw in fieldArr)
        {
            var field = fieldRaw.Trim();
            var idxSpace = field.IndexOf(' ');
            var propName = idxSpace == -1 ? field : field[..idxSpace];
            if (propertyMapping.ContainsKey(propName))
            {
                continue;
            }

            ProcessingResult.AddErrorMessage($"Cannot find property name for sort: {propName}");
            return false;
        }

        return true;
    }
}