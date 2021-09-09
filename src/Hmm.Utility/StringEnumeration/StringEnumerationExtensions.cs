using System;

namespace Hmm.Utility.StringEnumeration
{
    public static class StringEnumerationExtensions
    {
        public static string GetString(this Enum stringEnum)
        {
            return StringEnum.GetStringValue(stringEnum);
        }
    }
}