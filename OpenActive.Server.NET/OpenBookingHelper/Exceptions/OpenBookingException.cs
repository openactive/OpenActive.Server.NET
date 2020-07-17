using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using OpenActive.NET;
using System.Net;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    /// <summary>
    /// All errors thrown within OpenActive.Server.NET will subclass OpenBookingException,
    /// Which allows them to be rendered as a reponse using ToOpenActiveString() and GetHttpStatusCode().
    /// 
    /// The OpenBookingError classes from OpenActive.NET provide OpenActive-compliant names and response codes
    /// </summary>
    [Serializable()]
    public class OpenBookingException : Exception
    {
        public OpenBookingError OpenBookingError { get; }

        protected OpenBookingException()
           : base()
        { }

        /// <summary>
        /// Create an OpenBookingError
        /// 
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate OpenBookingError</param>
        public OpenBookingException(OpenBookingError error) :
           base($"{error.Type}: {error.Name}: {error.Description}")
        {
            this.OpenBookingError = error;
        }

        /// <summary>
        /// Create an OpenBookingError with a message specific to the instance of the problem
        /// 
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate OpenBookingError</param>
        /// <param name="message">A message that overwrites the the `Description` property of the supplied error</param>
        public OpenBookingException(OpenBookingError error, string message)
           : base($"{error.Type}: {error.Name}: {message}")
        {
            error.Description = message;
            this.OpenBookingError = error;
        }

        /// <summary>
        /// Create an OpenBookingError with a message specific to the instance of the problem, while maintaining any source exception.
        /// 
        /// Note that error.Name and error.StatusCode are set automatically by OpenActive.NET for each error type.
        /// </summary>
        /// <param name="error">The appropriate OpenBookingError</param>
        /// <param name="message">A message that overwrites the the `Description` property of the supplied error</param>
        /// <param name="innerException">The source exception</param>
        public OpenBookingException(OpenBookingError error, string message, Exception innerException) :
           base($"{error.Type}: {error.Name}: {message}", innerException)
        {
            error.Description = message;
            this.OpenBookingError = error;
        }

        /// <summary>
        /// Serialised the associated error to OpenActive compliant s JSON-LD
        /// 
        /// TODO: Should this just return the type, to allow it to be serialised by the application? Requires json type
        /// </summary>
        /// <returns>OpenActive compliant serialised JSON-LD</returns>
        private string ResponseJson
        {
            get
            {
                if (this.OpenBookingError == null)
                {
                    throw new NullReferenceException("An instance of OpenBookingException does not have an associated OpenBookingError");
                }
                else
                {
                    return OpenActiveSerializer.Serialize(this.OpenBookingError);
                }
            }
        }

        public ResponseContent ErrorResponseContent
        {
            get
            {
                return ResponseContent.OpenBookingErrorResponse(this.ResponseJson, this.HttpStatusCode);
            }
        }

        /// <summary>
        /// Get the HTTP status code assocaited with this error
        /// </summary>
        /// <returns>Associated status code</returns>
        private HttpStatusCode HttpStatusCode
        {
            get
            {
                if (!this.OpenBookingError.StatusCode.HasValue)
                {
                    // Default to 500 if not defined
                    return HttpStatusCode.InternalServerError;
                }
                else
                {
                    return (HttpStatusCode)this.OpenBookingError.StatusCode.Value;
                }
            }
        }

        protected OpenBookingException(SerializationInfo info,
                                    StreamingContext context)
           : base(info, context)
        { }
    }
}
