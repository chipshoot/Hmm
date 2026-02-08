using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.Services;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;

public class PropertyMappingService : IPropertyMappingService
{
    private readonly Dictionary<string, PropertyMappingValue> _gasLogPropertyMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        {"id", new PropertyMappingValue(new List<string> {"Id"})},
        {"automobileId", new PropertyMappingValue(new List<string> {"AutomobileId"})},
        {"date", new PropertyMappingValue(new List<string> {"Date"})},
        {"createDate", new PropertyMappingValue(new List<string> {"CreateDate"})},
        {"distance", new PropertyMappingValue(new List<string> {"Distance"})},
        {"odometer", new PropertyMappingValue(new List<string> {"Odometer"})},
        {"fuel", new PropertyMappingValue(new List<string> {"Fuel"})},
        {"totalPrice", new PropertyMappingValue(new List<string> {"TotalPrice"})},
        {"unitPrice", new PropertyMappingValue(new List<string> {"UnitPrice"})},
        {"comment", new PropertyMappingValue(new List<string> {"Comment"})}
    };

    private readonly IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

    public PropertyMappingService()
    {
        _propertyMappings.Add(new PropertyMapping<ApiGasLog, GasLog>(_gasLogPropertyMapping));
    }

    public ProcessingResult<Dictionary<string, PropertyMappingValue>> GetPropertyMapping<TSource, TDestination>()
    {
        var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>().ToList();

        if (matchingMapping.Any())
        {
            return ProcessingResult<Dictionary<string, PropertyMappingValue>>.Ok(matchingMapping.First().MappingDictionary);
        }

        var errMsg = $"Cannot find exact property mapping instance for <{typeof(TSource)}, {typeof(TDestination)}>";
        return ProcessingResult<Dictionary<string, PropertyMappingValue>>.NotFound(errMsg);
    }

    public ProcessingResult<bool> ValidMappingExistsFor<TSource, TDestination>(string fields)
    {
        if (string.IsNullOrEmpty(fields))
        {
            return ProcessingResult<bool>.Ok(true);
        }

        var propertyMappingResult = GetPropertyMapping<TSource, TDestination>();
        if (!propertyMappingResult.Success)
        {
            return ProcessingResult<bool>.Fail(propertyMappingResult.ErrorMessage, propertyMappingResult.ErrorType);
        }

        var propertyMapping = propertyMappingResult.Value;
        var fieldArr = fields.Split(',');
        var missingProperties = new List<string>();

        foreach (var fieldRaw in fieldArr)
        {
            var field = fieldRaw.Trim();
            var idxSpace = field.IndexOf(' ');
            var propName = idxSpace == -1 ? field : field[..idxSpace];
            if (!propertyMapping.ContainsKey(propName))
            {
                missingProperties.Add(propName);
            }
        }

        if (missingProperties.Count > 0)
        {
            var errorMessage = $"Cannot find the following property mappings: {string.Join(", ", missingProperties)}";
            return ProcessingResult<bool>.Invalid(errorMessage);
        }

        return ProcessingResult<bool>.Ok(true);
    }
}
