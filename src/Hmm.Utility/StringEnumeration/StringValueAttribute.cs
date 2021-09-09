using System;

namespace Hmm.Utility.StringEnumeration
{
    /// <summary>
    /// This property can be decorate on item of enumerated type to support string
    /// of the item
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
    [AttributeUsage(AttributeTargets.Field)]
    public class StringValueAttribute : Attribute
    {
        public StringValueAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}