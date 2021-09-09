using Hmm.Utility.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hmm.Utility.StringEnumeration

{
    /// <summary>
    /// Helper class for working with 'extended' enumerations using <see cref="StringValueAttribute"/> attributes.
    /// </summary>
    /// <remarks>
    /// To use StringEnumeration, first apply StringValueAttribute on items of enumerated type
    /// <code>
    ///     public enum HandTools
    ///     {
    ///         [StringValue("Cordless Power Drill")]
    ///         Drill = 5,
    ///         [StringValue("Long nose pliers")]
    ///         Pliers = 7,
    ///         [StringValue("20mm Chisel")]
    ///         Chisel = 9
    ///     }
    /// </code>
    /// then we can get the string of each item of enumerated type
    /// <code>
    ///     string itemName = StringEnum.GetStringValue(HandTools.Drill)
    /// </code>
    /// or we can convert a string (case insensitive) to enumerated type
    /// <code>
    ///     var tool = (HandTools)StringEnum.Parse(typeof(HandTools), toolName, true);
    /// </code>
    /// then Set valuable as enumerated type
    /// <code>
    ///     HandTools tool = HandTools.Drill;
    /// </code>
    /// </remarks>
    public class StringEnum
    {
        #region private fields

        private readonly Type _enumType;
        private static readonly Hashtable StringValues = new Hashtable();

        #endregion private fields

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="StringEnum"/> class.
        /// </summary>
        /// <param name="enumType">Enumerated type.</param>
        public StringEnum(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Supplied type must be an enumerated type was {0}", enumType);
                throw new ArgumentException(message);
            }

            _enumType = enumType;
        }

        #endregion constructor

        #region public properties

        /// <summary>
        /// Gets the underlying enumerated type for this instance.
        /// </summary>
        /// <value></value>
        public Type EnumType
        {
            get { return _enumType; }
        }

        #endregion public properties

        #region public static methods

        /// <summary>
        /// Gets a string value for a particular enumeration value.
        /// </summary>
        /// <param name="value">Value of the enum item</param>
        /// <returns>String Value associated via a <see cref="StringValueAttribute"/> attribute, or null if not found.</returns>
        public static string GetStringValue(Enum value)
        {
            string output = null;
            var type = value.GetType();

            if (StringValues.ContainsKey(value))
            {
                output = ((StringValueAttribute)StringValues[value]).Value;
            }
            else
            {
                // Look for our 'StringValueAttribute' in the field's custom attributes
                var fi = type.GetField(value.ToString());
                var attribute = fi.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
                if (attribute != null)
                {
                    if (attribute.Length > 0)
                    {
                        StringValues.Add(value, attribute[0]);
                        output = attribute[0].Value;
                    }
                }
                else
                {
                    output = value.ToString();
                }
            }

            return output;
        }

        /// <summary>
        /// Parses the supplied enumerated and string value to find an associated enumerated value.
        /// </summary>
        /// <param name="type">The type of the enum</param>
        /// <param name="stringValue">String value of enum item</param>
        /// <param name="ignoreCase">Denotes whether to conduct a case-insensitive match on the supplied string value</param>
        /// <param name="useDefaultIfNoMatch">If cannot parse the string, then return NoSet as default</param>
        /// <returns>Enumerated value associated with the string value, or null if not found.</returns>
        public static object Parse(Type type, string stringValue, bool ignoreCase = false, bool useDefaultIfNoMatch = false)
        {
            object output = null;
            string enumStringValue = null;

            if (!type.IsEnum)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Supplied type must be an Enumerated. Type was {0}", type));
            }

            // Look for our string value associated with fields in this enumeration
            foreach (var fi in type.GetFields())
            {
                // Check for our custom attribute
                var attrs = fi.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
                if (attrs?.Length > 0)
                {
                    enumStringValue = attrs[0].Value;
                }

                // Check for equality then select actual enumerated value.
                if (string.Compare(enumStringValue, stringValue, ignoreCase, CultureInfo.InvariantCulture) != 0)
                {
                    continue;
                }

                output = Enum.Parse(type, fi.Name);
                break;
            }

            if (output == null && useDefaultIfNoMatch)
            {
                output = (Enum)Activator.CreateInstance(type);
            }

            return output;
        }

        /// <summary>
        /// Return the existence of the given string value within the enumeration.
        /// </summary>
        /// <param name="enumType">Type of enumeration</param>
        /// <param name="stringValue">String value.</param>
        /// <returns>Existence of the string value</returns>
        public static bool IsStringDefined(Type enumType, string stringValue)
        {
            return Parse(enumType, stringValue) != null;
        }

        /// <summary>
        /// Return the existence of the given string value within the enumeration.
        /// </summary>
        /// <param name="enumType">Type of enumeration</param>
        /// <param name="stringValue">String value.</param>
        /// <param name="ignoreCase">Denotes whether to conduct a case-insensitive match on the supplied string value</param>
        /// <returns>Existence of the string value</returns>
        public static bool IsStringDefined(Type enumType, string stringValue, bool ignoreCase)
        {
            return Parse(enumType, stringValue, ignoreCase) != null;
        }

        /// <summary>
        /// Gets the string list from enum.
        /// </summary>
        /// <typeparam name="T">the type of enum</typeparam>
        /// <returns>all name of the enum</returns>
        public static ICollection<string> GetEnumStringList<T>() where T : struct
        {
            Guard.Against<ArgumentException>(!typeof(T).IsEnum, "Cannot get list for non enum type");

            var types = new StringEnum(typeof(T));
            var ret = (types.GetStringValues()).OfType<string>().ToList();
            return ret;
        }

        #endregion public static methods

        #region public methods

        /// <summary>
        /// Gets the string value associated with the given enumerated value.
        /// </summary>
        /// <param name="valueName">Name of the enumerated value.</param>
        /// <returns>string value</returns>
        public string GetStringValue(string valueName)
        {
            var enumType = (Enum)Enum.Parse(_enumType, valueName);
            var stringValue = GetStringValue(enumType);

            return stringValue;
        }

        /// <summary>
        /// Gets the string values associated with the enumeration.
        /// </summary>
        /// <returns>string value array</returns>
        public Array GetStringValues()
        {
            var values = new ArrayList();

            // Look for our string value associated with fields in this enumeration
            foreach (var fi in _enumType.GetFields())
            {
                // Check for our custom attribute
                var attrs = fi.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
                if (attrs != null)
                {
                    if (attrs.Length > 0)
                    {
                        values.Add(attrs[0].Value);
                    }
                }
            }

            return values.ToArray();
        }

        /// <summary>
        /// Gets the values as a 'bindable' list data source.
        /// </summary>
        /// <returns>IList for data binding</returns>
        public IList GetListValues()
        {
            var underlyingType = Enum.GetUnderlyingType(_enumType);
            var values = new ArrayList();

            // Look for our string value associated with fields in this enumeration
            foreach (var fi in _enumType.GetFields())
            {
                // Check for our custom attribute
                var attributes = fi.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
                if (attributes != null)
                {
                    if (attributes.Length > 0)
                    {
                        values.Add(new DictionaryEntry(Convert.ChangeType(Enum.Parse(_enumType, fi.Name), underlyingType, CultureInfo.InvariantCulture), attributes[0].Value));
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Return the existence of the given string value within the enumeration.
        /// </summary>
        /// <param name="stringValue">string value.</param>
        /// <returns>Existence of the string value</returns>
        public bool IsStringDefined(string stringValue)
        {
            return Parse(_enumType, stringValue) != null;
        }

        /// <summary>
        /// Return the existence of the given string value within the enumeration.
        /// </summary>
        /// <param name="stringValue">String value.</param>
        /// <param name="ignoreCase">Denotes whether to conduct a case-insensitive match on the supplied string value</param>
        /// <returns>Existence of the string value</returns>
        public bool IsStringDefined(string stringValue, bool ignoreCase)
        {
            return Parse(_enumType, stringValue, ignoreCase) != null;
        }

        #endregion public methods
    }
}