using System;
using System.Runtime.Serialization;
using OpenActive.NET;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    /// <summary>
    /// All internal errors, caused by unexpected system behaviour, thrown within OpenActive.Server.NET will subclass InternalOpenBookingException,
    /// This allows them to be caught and logged separately to OpenBookingException.
    ///
    /// The InternalOpenBookingError classes from OpenActive.NET provide OpenActive-compliant names and response codes
    /// </summary>
    [Serializable]
    public class InternalOpenBookingException : OpenBookingException
    {
        protected InternalOpenBookingException()
        {
        }

        /// <summary>
        /// Create an InternalOpenBookingError
        ///
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate InternalOpenBookingError</param>
        public InternalOpenBookingException(InternalOpenBookingError error)
            : base(error)
        {
        }

        /// <summary>
        /// Create an InternalOpenBookingError with a message specific to the instance of the problem
        ///
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate InternalOpenBookingError</param>
        /// <param name="message">A message that overwrites the the `Description` property of the supplied error</param>
        public InternalOpenBookingException(InternalOpenBookingError error, string message)
            : base(error, message)
        {
        }

        /// <summary>
        /// Create an InternalOpenBookingError with a message specific to the instance of the problem, while maintaining any source exception.
        ///
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate InternalOpenBookingError</param>
        /// <param name="message">A message that overwrites the the `Description` property of the supplied error</param>
        /// <param name="innerException">The source exception</param>
        public InternalOpenBookingException(InternalOpenBookingError error, string message, Exception innerException) :
            base(error, message, innerException)
        {
        }

        protected InternalOpenBookingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
