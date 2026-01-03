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
        BusinessRuleViolation = 6,

        /// <summary>
        /// The entity has been deleted (maps to HTTP 410)
        /// </summary>
        Deleted = 7,

        /// <summary>
        /// The user is not authorized to perform the action (maps to HTTP 401)
        /// </summary>
        Unauthorized = 8,

        /// <summary>
        /// The request could not be completed due to a conflict with the current state of the resource (maps to HTTP 409)
        /// </summary>
        Conflict = 9,
    }
}
