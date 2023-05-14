using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching;

namespace BookingSystem.AspNetCore.Helpers
{
    public static class ResponseContentHelper
    {
        public static Microsoft.AspNetCore.Mvc.ContentResult GetContentResult(this OpenActive.Server.NET.OpenBookingHelper.ResponseContent response)
        {
            return new CacheableContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = response.Content,
                ContentType = response.ContentType,
                CacheControlMaxAge = response.CacheControlMaxAge,
            };
        }
    }

    /// <summary>
    /// ContentResult that also sets Cache-Control: public, max-age=X, s-maxage=X
    /// See https://developer.openactive.io/publishing-data/data-feeds/scaling-feeds for more information
    /// </summary>
    public class CacheableContentResult : Microsoft.AspNetCore.Mvc.ContentResult
    {
        public TimeSpan? CacheControlMaxAge { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (CacheControlMaxAge != null)
            {
                context.HttpContext.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = CacheControlMaxAge.Value,
                        SharedMaxAge = CacheControlMaxAge.Value,
                    };
            }

            await base.ExecuteResultAsync(context);
        }
    }
}
