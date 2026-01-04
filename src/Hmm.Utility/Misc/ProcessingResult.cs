using Hmm.Utility.Dal.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Misc
{
    /// <summary>
    /// Represents the absence of a value for Result&lt;T&gt; when no value is needed.
    /// Use Result&lt;Unit&gt; for operations that don't return meaningful data.
    /// </summary>
    public readonly struct Unit
    {
        public static readonly Unit Value = new();
    }

    /// <summary>
    /// Immutable result pattern for all operations.
    /// Thread-safe and prevents race conditions through immutability.
    /// Eliminates the need for "return null" and makes error handling explicit.
    ///
    /// Use Result&lt;T&gt; where T is the return type.
    /// For operations with no return value, use Result&lt;Unit&gt;.
    /// </summary>
    /// <typeparam name="T">The type of value returned on success, or Unit if no value is needed</typeparam>
    public class ProcessingResult<T>
    {
        private readonly IReadOnlyList<ReturnMessage> _messages;

        private ProcessingResult(bool success, T value, IEnumerable<ReturnMessage> messages, ErrorCategory errorType)
        {
            Success = success;
            Value = value;
            _messages = messages?.ToList().AsReadOnly() ?? new List<ReturnMessage>().AsReadOnly();
            ErrorType = errorType;
        }

        /// <summary>
        /// Indicates whether the operation succeeded
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The result value (only valid when Success is true)
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Read-only collection of messages
        /// </summary>
        public IReadOnlyList<ReturnMessage> Messages => _messages;

        /// <summary>
        /// Type of error if operation failed
        /// </summary>
        public ErrorCategory ErrorType { get; }

        /// <summary>
        /// Primary error message (first error in the list)
        /// </summary>
        public string ErrorMessage => _messages.FirstOrDefault(m => m.Type == MessageType.Error)?.Message ?? string.Empty;

        /// <summary>
        /// Indicates whether the result represents a "not found" state.
        /// Returns true for both NotFound() and EmptyOk() results.
        /// Check Success property to distinguish: Success=true means expected absence, Success=false means error.
        /// </summary>
        public bool IsNotFound => ErrorType == ErrorCategory.NotFound;

        public bool HasInfo => _messages.Any(m => m.Type == MessageType.Info);

        public bool HasWarning => _messages.Any(m => m.Type == MessageType.Warning);

        public bool HasError => _messages.Any(m => m.Type is MessageType.Error or MessageType.Fatal);

        public bool HasFatal => _messages.Any(m => m.Type == MessageType.Fatal);

        /// <summary>
        /// Creates a successful result with a value
        /// </summary>
        public static ProcessingResult<T> Ok(T value)
            => new(true, value, Array.Empty<ReturnMessage>(), ErrorCategory.None);

        /// <summary>
        /// Creates a successful result with a value and info messages
        /// </summary>
        public static ProcessingResult<T> Ok(T value, params string[] infoMessages)
        {
            var messages = infoMessages.Select(m => new ReturnMessage { Message = m, Type = MessageType.Info });
            return new ProcessingResult<T>(true, value, messages, ErrorCategory.None);
        }

        /// <summary>
        /// Creates a successful result with no value (for queries where absence is normal).
        /// Use this when "not found" is an expected, valid outcome rather than an error.
        /// Example: Optional lookups, checking existence before creation, search queries with no results.
        /// Note: IsNotFound will return true for this result, but Success will also be true.
        /// </summary>
        public static ProcessingResult<T> EmptyOk(string message = "No data found")
        {
            var msg = new ReturnMessage { Message = message, Type = MessageType.Info };
            return new ProcessingResult<T>(true, default, new[] { msg }, ErrorCategory.NotFound);
        }

        /// <summary>
        /// Creates a failure result indicating resource not found.
        /// Use this when the operation REQUIRES the resource to exist (e.g., Update, Delete).
        /// For queries where absence is normal, use EmptyOk() instead.
        /// Note: IsNotFound will return true, and Success will be false.
        /// </summary>
        public static ProcessingResult<T> NotFound(string message = "Resource not found")
        {
            var msg = new ReturnMessage { Message = message, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.NotFound);
        }

        /// <summary>
        /// Creates a failure result indicating resource was deleted
        /// </summary>
        public static ProcessingResult<T> Deleted(string message = "Resource has been deleted")
        {
            var msg = new ReturnMessage { Message = message, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.Deleted);
        }

        /// <summary>
        /// Creates a failure result for validation errors
        /// </summary>
        public static ProcessingResult<T> Invalid(string validationError)
        {
            var msg = new ReturnMessage { Message = validationError, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.ValidationError);
        }

        /// <summary>
        /// Creates a failure result for validation errors
        /// </summary>
        public static ProcessingResult<T> Invalid(params string[] validationErrors)
        {
            var messages = validationErrors.Select(m => new ReturnMessage { Message = m, Type = MessageType.Error });
            return new ProcessingResult<T>(false, default, messages, ErrorCategory.ValidationError);
        }

        /// <summary>
        /// Creates a failure result for unauthorized access
        /// </summary>
        public static ProcessingResult<T> Unauthorized(string message = "Unauthorized access")
        {
            var msg = new ReturnMessage { Message = message, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.Unauthorized);
        }

        /// <summary>
        /// Creates a failure result for conflicts (e.g., duplicate keys)
        /// </summary>
        public static ProcessingResult<T> Conflict(string message)
        {
            var msg = new ReturnMessage { Message = message, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.Conflict);
        }

        /// <summary>
        /// Creates a failure result from an exception
        /// </summary>
        public static ProcessingResult<T> FromException(Exception ex)
        {
            var msg = new ReturnMessage { Message = ex.GetAllMessage(), Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, ErrorCategory.ServerError);
        }

        /// <summary>
        /// Creates a general failure result
        /// </summary>
        public static ProcessingResult<T> Fail(string errorMessage, ErrorCategory errorType = ErrorCategory.ServerError)
        {
            var msg = new ReturnMessage { Message = errorMessage, Type = MessageType.Error };
            return new ProcessingResult<T>(false, default, new[] { msg }, errorType);
        }

        /// <summary>
        /// Adds a warning message and returns a new result (immutable)
        /// </summary>
        public ProcessingResult<T> WithWarning(string warningMessage)
        {
            var newMessages = _messages.Concat(new[] { new ReturnMessage { Message = warningMessage, Type = MessageType.Warning } });
            return new ProcessingResult<T>(Success, Value, newMessages, ErrorType);
        }

        /// <summary>
        /// Adds an info message and returns a new result (immutable)
        /// </summary>
        public ProcessingResult<T> WithInfo(string infoMessage)
        {
            var newMessages = _messages.Concat(new[] { new ReturnMessage { Message = infoMessage, Type = MessageType.Info } });
            return new ProcessingResult<T>(Success, Value, newMessages, ErrorType);
        }

        /// <summary>
        /// Adds an error message and returns a new result (immutable)
        /// </summary>
        public ProcessingResult<T> WithError(string errorMessage)
        {
            var newMessages = _messages.Concat(new[] { new ReturnMessage { Message = errorMessage, Type = MessageType.Error } });
            return new ProcessingResult<T>(false, Value, newMessages, ErrorCategory.ValidationError);
        }

        /// <summary>
        /// Combines this result with another result (immutable).
        /// Combined result is successful only if both results are successful.
        /// Messages from both results are merged.
        /// </summary>
        public ProcessingResult<T> Combine(ProcessingResult<T> other)
        {
            if (other == null)
            {
                return this;
            }

            var combinedMessages = _messages.Concat(other._messages);
            var combinedSuccess = Success && other.Success;
            var combinedErrorType = !combinedSuccess
                ? (ErrorType != ErrorCategory.None ? ErrorType : other.ErrorType)
                : ErrorCategory.None;

            return new ProcessingResult<T>(combinedSuccess, Value, combinedMessages, combinedErrorType);
        }

        /// <summary>
        /// Logs all messages using the provided logger
        /// </summary>
        public void LogMessages(ILogger logger)
        {
            if (logger == null || !_messages.Any())
            {
                return;
            }

            foreach (var msg in _messages)
            {
                switch (msg.Type)
                {
                    case MessageType.Info:
                        logger.LogInformation(msg.Message);
                        break;

                    case MessageType.Error:
                    case MessageType.Fatal:
                        logger.LogError(msg.Message);
                        break;

                    case MessageType.Warning:
                        logger.LogWarning(msg.Message);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets all messages as a single formatted string
        /// </summary>
        public string GetWholeMessage()
        {
            return _messages.Count switch
            {
                0 => string.Empty,
                1 => $"{_messages[0].Type}: {_messages[0].Message}",
                _ => string.Join(" | ", _messages.Select(m => $"{m.Type}: {m.Message}"))
            };
        }

        /// <summary>
        /// Implicit conversion to bool for convenience
        /// </summary>
        public static implicit operator bool(ProcessingResult<T> result) => result.Success;
    }
}