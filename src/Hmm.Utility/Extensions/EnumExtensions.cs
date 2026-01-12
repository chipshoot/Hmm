using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Hmm.Utility.Extensions
{
    /// <summary>
    /// Modern extension methods for working with enumerations using the standard Display attribute.
    /// This replaces the legacy StringEnum pattern with a more maintainable, thread-safe, and performant approach.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    ///     public enum HandTools
    ///     {
    ///         [Display(Name = "Cordless Power Drill")]
    ///         Drill = 5,
    ///         [Display(Name = "Long nose pliers")]
    ///         Pliers = 7
    ///     }
    ///
    ///     // Get display name
    ///     string name = HandTools.Drill.GetDisplayName();
    ///
    ///     // Parse from display name
    ///     var tool = EnumExtensions.ParseByDisplayName&lt;HandTools&gt;("Cordless Power Drill");
    ///
    ///     // Get all display names
    ///     var names = EnumExtensions.GetDisplayNames&lt;HandTools&gt;();
    /// </code>
    /// </remarks>
    public static class EnumExtensions
    {
        // Thread-safe cache for enum metadata
        private static readonly ConcurrentDictionary<Type, Dictionary<string, object>> DisplayNameToValueCache
            = new ConcurrentDictionary<Type, Dictionary<string, object>>();

        private static readonly ConcurrentDictionary<Enum, string> ValueToDisplayNameCache
            = new ConcurrentDictionary<Enum, string>();

        /// <summary>
        /// Gets the display name for an enum value from the Display attribute, or returns the enum name if no attribute is present.
        /// </summary>
        /// <param name="value">The enum value</param>
        /// <returns>Display name from Display attribute, or the enum value name if not found</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static string GetDisplayName(this Enum value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return ValueToDisplayNameCache.GetOrAdd(value, v =>
            {
                var type = v.GetType();
                var memberInfo = type.GetMember(v.ToString()).FirstOrDefault();

                if (memberInfo == null)
                    return v.ToString();

                var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
                return displayAttribute?.Name ?? v.ToString();
            });
        }

        /// <summary>
        /// Gets the display description for an enum value from the Display attribute.
        /// </summary>
        /// <param name="value">The enum value</param>
        /// <returns>Description from Display attribute, or null if not found</returns>
        /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
        public static string GetDisplayDescription(this Enum value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString()).FirstOrDefault();

            if (memberInfo == null)
                return null;

            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
            return displayAttribute?.Description;
        }

        /// <summary>
        /// Parses a display name to its corresponding enum value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="displayName">The display name to parse</param>
        /// <param name="ignoreCase">Whether to ignore case when comparing</param>
        /// <returns>The enum value</returns>
        /// <exception cref="ArgumentException">Thrown when TEnum is not an enum type or displayName is not found</exception>
        /// <exception cref="ArgumentNullException">Thrown when displayName is null</exception>
        public static TEnum ParseByDisplayName<TEnum>(string displayName, bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentNullException(nameof(displayName));

            var type = typeof(TEnum);
            var lookup = GetDisplayNameLookup(type, ignoreCase);

            var comparer = ignoreCase
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            var key = lookup.Keys.FirstOrDefault(k => comparer.Equals(k, displayName));

            if (key != null && lookup.TryGetValue(key, out var value))
                return (TEnum)value;

            throw new ArgumentException(
                $"Display name '{displayName}' was not found in enum type {type.Name}",
                nameof(displayName));
        }

        /// <summary>
        /// Tries to parse a display name to its corresponding enum value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="displayName">The display name to parse</param>
        /// <param name="result">The parsed enum value if successful</param>
        /// <param name="ignoreCase">Whether to ignore case when comparing</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public static bool TryParseByDisplayName<TEnum>(string displayName, out TEnum result, bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            result = default;

            if (string.IsNullOrEmpty(displayName))
                return false;

            try
            {
                result = ParseByDisplayName<TEnum>(displayName, ignoreCase);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a display name exists in the enum.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <param name="displayName">The display name to check</param>
        /// <param name="ignoreCase">Whether to ignore case when comparing</param>
        /// <returns>True if the display name exists, false otherwise</returns>
        public static bool IsDisplayNameDefined<TEnum>(string displayName, bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            return TryParseByDisplayName<TEnum>(displayName, out _, ignoreCase);
        }

        /// <summary>
        /// Gets all display names for an enum type.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <returns>Collection of display names</returns>
        public static IReadOnlyCollection<string> GetDisplayNames<TEnum>()
            where TEnum : struct, Enum
        {
            var type = typeof(TEnum);
            var lookup = GetDisplayNameLookup(type, ignoreCase: false);
            return lookup.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets all enum values with their display names as key-value pairs.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <returns>Dictionary mapping display names to enum values</returns>
        public static IReadOnlyDictionary<string, TEnum> GetDisplayNameDictionary<TEnum>()
            where TEnum : struct, Enum
        {
            var type = typeof(TEnum);
            var lookup = GetDisplayNameLookup(type, ignoreCase: false);

            return lookup.ToDictionary(
                kvp => kvp.Key,
                kvp => (TEnum)kvp.Value
            );
        }

        /// <summary>
        /// Gets all enum values with their integer values as key-value pairs.
        /// </summary>
        /// <typeparam name="TEnum">The enum type</typeparam>
        /// <returns>Dictionary mapping integer values to display names</returns>
        public static IReadOnlyDictionary<int, string> GetValueDisplayNameDictionary<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .ToDictionary(
                    e => Convert.ToInt32(e),
                    e => (e as Enum).GetDisplayName()
                );
        }

        /// <summary>
        /// Clears all internal caches. Use this if enum types are loaded/unloaded dynamically.
        /// </summary>
        public static void ClearCache()
        {
            DisplayNameToValueCache.Clear();
            ValueToDisplayNameCache.Clear();
        }

        #region Private Helper Methods

        private static Dictionary<string, object> GetDisplayNameLookup(Type enumType, bool ignoreCase)
        {
            var cacheKey = ignoreCase
                ? Type.GetType($"{enumType.FullName}_IgnoreCase") ?? enumType
                : enumType;

            return DisplayNameToValueCache.GetOrAdd(cacheKey, t =>
            {
                var lookup = new Dictionary<string, object>(
                    ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    var value = field.GetValue(null);
                    if (value == null)
                        continue;

                    var displayAttribute = field.GetCustomAttribute<DisplayAttribute>(false);
                    var displayName = displayAttribute?.Name ?? field.Name;

                    // Store both the display name and the actual enum name for fallback
                    lookup[displayName] = value;

                    // Also allow lookup by enum member name if different from display name
                    if (displayName != field.Name && !lookup.ContainsKey(field.Name))
                    {
                        lookup[field.Name] = value;
                    }
                }

                return lookup;
            });
        }

        #endregion
    }
}
