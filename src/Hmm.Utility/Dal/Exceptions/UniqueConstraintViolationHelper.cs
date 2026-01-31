using System;

namespace Hmm.Utility.Dal.Exceptions
{
    /// <summary>
    /// Helper class for detecting and handling unique constraint violations from database exceptions.
    ///
    /// This class addresses Issue #43 (Race Condition in Uniqueness Validation) by providing
    /// a centralized way to detect when a database unique constraint has been violated.
    ///
    /// The race condition occurs when:
    /// 1. Thread A validates that a name is unique (query returns no results)
    /// 2. Thread B validates that the same name is unique (query returns no results)
    /// 3. Thread A inserts the record
    /// 4. Thread B attempts to insert the same record - violates unique constraint
    ///
    /// By catching the unique constraint violation and converting it to a user-friendly error,
    /// we handle this race condition gracefully without relying solely on application-level validation.
    ///
    /// Note: This helper uses type name and message matching to avoid requiring direct references
    /// to database-specific assemblies (Npgsql, SqlClient, EF Core).
    /// </summary>
    public static class UniqueConstraintViolationHelper
    {
        /// <summary>
        /// PostgreSQL error code for unique constraint violation.
        /// </summary>
        private const string PostgresUniqueViolationCode = "23505";

        /// <summary>
        /// SQL Server error numbers for unique constraint/index violations.
        /// </summary>
        private const int SqlServerUniqueViolationNumber = 2627;
        private const int SqlServerUniqueIndexViolationNumber = 2601;

        /// <summary>
        /// Determines if the exception is caused by a unique constraint violation.
        /// Supports PostgreSQL and SQL Server databases.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the exception represents a unique constraint violation; otherwise, false.</returns>
        public static bool IsUniqueConstraintViolation(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            // Check by exception type name to avoid requiring direct assembly references
            var exTypeName = ex.GetType().Name;

            // DbUpdateException from EF Core
            if (exTypeName == "DbUpdateException")
            {
                return IsUniqueConstraintViolation(ex.InnerException);
            }

            // DataSourceException (custom wrapper)
            if (exTypeName == "DataSourceException")
            {
                return IsUniqueConstraintViolation(ex.InnerException);
            }

            // PostgresException from Npgsql
            if (exTypeName == "PostgresException")
            {
                // Use reflection to get SqlState property
                var sqlStateProperty = ex.GetType().GetProperty("SqlState");
                if (sqlStateProperty != null)
                {
                    var sqlState = sqlStateProperty.GetValue(ex) as string;
                    return sqlState == PostgresUniqueViolationCode;
                }
            }

            // SqlException from Microsoft.Data.SqlClient or System.Data.SqlClient
            if (exTypeName == "SqlException")
            {
                // Use reflection to get Number property
                var numberProperty = ex.GetType().GetProperty("Number");
                if (numberProperty != null)
                {
                    var number = (int)numberProperty.GetValue(ex);
                    return number == SqlServerUniqueViolationNumber ||
                           number == SqlServerUniqueIndexViolationNumber;
                }
            }

            // Check inner exception recursively
            if (ex.InnerException != null)
            {
                return IsUniqueConstraintViolation(ex.InnerException);
            }

            // Fallback: check exception message for common unique constraint patterns
            var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
            return message.Contains("unique constraint") ||
                   message.Contains("duplicate key") ||
                   message.Contains("unique index") ||
                   message.Contains("violates unique") ||
                   message.Contains("cannot insert duplicate");
        }

        /// <summary>
        /// Extracts the constraint name from a unique constraint violation exception if available.
        /// </summary>
        /// <param name="ex">The exception to extract the constraint name from.</param>
        /// <returns>The constraint name if found; otherwise, null.</returns>
        public static string GetConstraintName(Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            var exTypeName = ex.GetType().Name;

            if (exTypeName is "DbUpdateException" or "DataSourceException")
            {
                return GetConstraintName(ex.InnerException);
            }

            if (exTypeName == "PostgresException")
            {
                var constraintProperty = ex.GetType().GetProperty("ConstraintName");
                return constraintProperty?.GetValue(ex) as string;
            }

            if (ex.InnerException != null)
            {
                return GetConstraintName(ex.InnerException);
            }

            return null;
        }

        /// <summary>
        /// Creates a user-friendly error message for a unique constraint violation.
        /// </summary>
        /// <param name="entityType">The type of entity (e.g., "Author", "Tag").</param>
        /// <param name="fieldName">The name of the field that must be unique (e.g., "AccountName", "Name").</param>
        /// <param name="value">The duplicate value that was attempted.</param>
        /// <returns>A user-friendly error message.</returns>
        public static string CreateDuplicateErrorMessage(string entityType, string fieldName, string value)
        {
            return $"A {entityType.ToLowerInvariant()} with {fieldName} '{value}' already exists.";
        }
    }
}
