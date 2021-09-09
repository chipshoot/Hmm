using System;

namespace Hmm.Utility.Specification
{
    public class SpecificationException : Exception
    {/// <summary>
     /// Initializes a new instance of the <see cref="SpecificationException" /> class.
     /// </summary>
        public SpecificationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationException" /> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public SpecificationException(string errorMessage)
            : base(errorMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationException" /> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SpecificationException(string errorMessage, Exception innerException)
            : base(errorMessage, innerException)
        {
        }
    }
}