using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Services;

public static class PropertyMappingServiceHelpExtension
{
    public static string GetMappedSortClause(this string apiSortClause, Dictionary<string, PropertyMappingValue> propertyMappings)
    {
        if (string.IsNullOrEmpty(apiSortClause))
        {
            return string.Empty;
        }

        if (propertyMappings == null)
        {
            return string.Empty;
        }

        var orderBys = apiSortClause.Split(',');
        var orderByFinalText = string.Empty;
        foreach (var orderByClause in orderBys)
        {
            var orderByTrimmed = orderByClause.Trim();
            var orderDesc = orderByTrimmed.EndsWith(" desc");
            var idxSpace = orderByTrimmed.IndexOf(" ", StringComparison.Ordinal);
            var propName = idxSpace == -1 ? orderByTrimmed : orderByTrimmed.Remove(idxSpace);
            if (!propertyMappings.ContainsKey(propName))
            {
                throw new ArgumentException($"Key mapping for {propName} is missing");
            }

            var mappedProps = propertyMappings[propName];
            if (mappedProps == null)
            {
                throw new ArgumentNullException($"{propName}");
            }

            foreach (var mappedProp in mappedProps.DestinationProperties)
            {
                if (mappedProps.Revert)
                {
                    orderDesc = !orderDesc;
                }

                orderByFinalText = orderByFinalText +
                                   (string.IsNullOrEmpty(orderByFinalText) ? string.Empty : ", ") + mappedProp +
                                   (orderDesc ? " descending" : " ascending");
            }
        }

        return orderByFinalText;
    }
}