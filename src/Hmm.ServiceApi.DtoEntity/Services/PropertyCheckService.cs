using Hmm.Utility.Misc;
using System.Reflection;

namespace Hmm.ServiceApi.DtoEntity.Services;

public class PropertyCheckService : IPropertyCheckService
{
    public ProcessingResult<bool> TypeHasProperties<T>(string fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return ProcessingResult<bool>.Ok(true);
        }

        // The field is separated by ",", so we split it.
        var fieldsAfterSplit = fields.Split(',');
        var missingProperties = new System.Collections.Generic.List<string>();

        // Check if the requested fields exist on source
        foreach (var field in fieldsAfterSplit)
        {
            // Trim each field, as it might contain leading
            // or trailing spaces. Can't trim the var in foreach,
            // so use another var.
            var propertyName = field.Trim();

            // Use reflection to check if the property can be
            // found on T.
            var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            // It can't be found, add to missing properties list
            if (propertyInfo == null)
            {
                missingProperties.Add(propertyName);
            }
        }

        // If there are missing properties, return invalid result
        if (missingProperties.Count > 0)
        {
            var errorMessage = $"Cannot find the following properties on type {typeof(T).Name}: {string.Join(", ", missingProperties)}";
            return ProcessingResult<bool>.Invalid(errorMessage);
        }

        // All checks out, return true
        return ProcessingResult<bool>.Ok(true);
    }
}