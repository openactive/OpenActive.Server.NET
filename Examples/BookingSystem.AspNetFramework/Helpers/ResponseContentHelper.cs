using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace BookingSystem.AspNetFramework.Helpers
{
    public static class ResponseContentHelper
    {
        public static HttpResponseMessage GetContentResult(this OpenActive.Server.NET.OpenBookingHelper.ResponseContent response)
        {
            var resp = new HttpResponseMessage
            {
                Content = new StringContent(response.Content ?? ""),
                StatusCode = response.StatusCode
            };
            resp.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(response.ContentType);
            /// Sets additional header Cache-Control: public, max-age=X, s-maxage=X
            /// See https://developer.openactive.io/publishing-data/data-feeds/scaling-feeds for more information
            if (response.CacheControlMaxAge != null)
            {
                resp.Headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = response.CacheControlMaxAge,
                    SharedMaxAge = response.CacheControlMaxAge
                };
            } else {
                resp.Headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = false,
                    MaxAge = null,
                    SharedMaxAge = null
                };
            }
            // Ensure custom error messages do not override responses
            HttpContext.Current.Response.TrySkipIisCustomErrors = true;
            return resp;
        }
    }
}
