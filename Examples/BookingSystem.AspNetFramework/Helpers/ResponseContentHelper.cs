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
            // Ensure custom error messages do not override responses
            HttpContext.Current.Response.TrySkipIisCustomErrors = true;
            return resp;
        }
    }
}
