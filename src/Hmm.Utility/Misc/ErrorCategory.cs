namespace Hmm.Utility.Misc
{
    /// <summary>
    /// Categories of errors that map to HTTP status codes and error handling strategies
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// No error (success case)
        /// </summary>
        None = 0,

        /// <summary>
        /// Entity or resource not found (maps to HTTP 404)
        /// </summary>
        NotFound = 1,

        /// <summary>
        /// Validation error - invalid input (maps to HTTP 400)
        /// </summary>
        ValidationError = 2,

        /// <summary>
        /// Concurrency conflict - optimistic locking failure (maps to HTTP 409)
        /// </summary>
        ConcurrencyError = 3,

        /// <summary>
        /// Mapping error - AutoMapper or similar failure (maps to HTTP 500)
        /// </summary>
        MappingError = 4,

        /// <summary>
        /// Server error - unexpected error (maps to HTTP 500)
        /// </summary>
        ServerError = 5,

        /// <summary>
        /// Business rule violation (maps to HTTP 422)
        /// </summary>
        BusinessRuleViolation = 6
    }
}
