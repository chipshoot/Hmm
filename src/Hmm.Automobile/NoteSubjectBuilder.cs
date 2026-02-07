using System;
using System.Globalization;

namespace Hmm.Automobile
{
    /// <summary>
    /// Centralizes note subject string construction and parsing for automobile entities.
    /// Replaces fragile ad-hoc string concatenation with a consistent, well-defined format.
    /// </summary>
    /// <remarks>
    /// <para>Subject format: <c>{EntityType},{Key}:{Value}</c></para>
    /// <para>Example: <c>GasLog,AutomobileId:5</c></para>
    /// </remarks>
    public static class NoteSubjectBuilder
    {
        /// <summary>
        /// Delimiter between the entity type and key-value parameters.
        /// </summary>
        public const char ParameterDelimiter = ',';

        /// <summary>
        /// Delimiter between a parameter key and its value.
        /// </summary>
        public const char KeyValueDelimiter = ':';

        /// <summary>
        /// The parameter key used to store the automobile ID in a GasLog subject.
        /// </summary>
        public const string AutomobileIdKey = "AutomobileId";

        /// <summary>
        /// Builds a note subject for a gas log entry associated with a specific automobile.
        /// </summary>
        /// <param name="automobileId">The automobile ID to encode in the subject.</param>
        /// <returns>A subject string in the format <c>GasLog,AutomobileId:{id}</c>.</returns>
        public static string BuildGasLogSubject(int automobileId)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"{AutomobileConstant.GasLogRecordSubject}{ParameterDelimiter}{AutomobileIdKey}{KeyValueDelimiter}{automobileId}");
        }

        /// <summary>
        /// Attempts to extract the automobile ID from a note subject string.
        /// </summary>
        /// <param name="subject">The subject string to parse.</param>
        /// <param name="automobileId">When this method returns, contains the automobile ID if parsing succeeded.</param>
        /// <returns><c>true</c> if the automobile ID was successfully extracted; otherwise, <c>false</c>.</returns>
        public static bool TryParseAutomobileId(string subject, out int automobileId)
        {
            automobileId = 0;
            if (string.IsNullOrEmpty(subject))
            {
                return false;
            }

            // Find the AutomobileId parameter
            var paramStart = subject.IndexOf(
                $"{AutomobileIdKey}{KeyValueDelimiter}", StringComparison.Ordinal);
            if (paramStart < 0)
            {
                return false;
            }

            var valueStart = paramStart + AutomobileIdKey.Length + 1; // +1 for KeyValueDelimiter
            if (valueStart >= subject.Length)
            {
                return false;
            }

            // Value runs until next ParameterDelimiter or end of string
            var valueEnd = subject.IndexOf(ParameterDelimiter, valueStart);
            var valueSpan = valueEnd >= 0
                ? subject.AsSpan(valueStart, valueEnd - valueStart)
                : subject.AsSpan(valueStart);

            return int.TryParse(valueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out automobileId);
        }
    }
}
