using OpenActive.NET;
using System;
using System.Net;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    /// <summary>
    /// This is a .NET version agnostic representation of a result from the Booking Engine
    /// It includes a .NET version-specific helper functions that simplify integration with .NET Framework MVC
    /// A .NET Core MVC helper extension is also available at TODO: [Add URL]
    /// </summary>
    public class ResponseContent
    {
        public static ResponseContent HtmlResponse(string content)
        {
            return new ResponseContent
            {
                Content = content,
                ContentType = System.Net.Mime.MediaTypeNames.Text.Html,
                StatusCode = HttpStatusCode.OK
            };
        }

        public static ResponseContent OpenBookingResponse (string content, HttpStatusCode httpStatusCode)
        {
            return new ResponseContent
            {
                Content = content,
                ContentType = OpenActiveMediaTypes.OpenBooking.Version1,
                StatusCode = httpStatusCode
            };
        }

        public static ResponseContent OpenBookingNoContentResponse()
        {
            return new ResponseContent
            {
                Content = null,
                ContentType = OpenActiveMediaTypes.OpenBooking.Version1,
                StatusCode = HttpStatusCode.NoContent
            };
        }

        public static ResponseContent OpenBookingErrorResponse(string content, HttpStatusCode statusCode)
        {
            return new ResponseContent
            {
                Content = content,
                ContentType = OpenActiveMediaTypes.OpenBooking.Version1,
                StatusCode = statusCode
            };
        }

        public static ResponseContent OrdersFeedRpdeResponse(string content)
        {
            return new ResponseContent
            {
                Content = content,
                ContentType = OpenActiveMediaTypes.OpenBooking.Version1,
                StatusCode = HttpStatusCode.OK
            };
        }

        public static ResponseContent RpdeResponse(string content, TimeSpan cacheControlMaxAge)
        {
            return new ResponseContent
            {
                Content = content,
                ContentType = OpenActiveMediaTypes.RealtimePagedDataExchange.Version1,
                StatusCode = HttpStatusCode.OK,
                CacheControlMaxAge = cacheControlMaxAge
            };
        }

        //
        // Summary:
        //     A string representing the JSON response
        public string Content { get; internal set; }
        //
        // Summary:
        //     The default Open Booking API content type for the version of Open Booking supported by the SDK
        public string ContentType { get; internal set; } = OpenActiveMediaTypes.OpenBooking.Version1;
        //
        // Summary:
        //     The intended HTTP status code of the response
        public HttpStatusCode StatusCode { get; internal set; } = HttpStatusCode.OK;
        //
        // Summary:
        //     The Cache-Control header intended for the response (public, max-age=X)
        public TimeSpan CacheControlMaxAge { get; internal set; }

        public override string ToString()
        {
            return this.Content;
        }
    }
}
