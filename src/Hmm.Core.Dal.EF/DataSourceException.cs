using System;

namespace Hmm.Core.Dal.EF
{
    public class DataSourceException : Exception
    {
        public DataSourceException(string message) : base(message)
        {
        }

        public DataSourceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}