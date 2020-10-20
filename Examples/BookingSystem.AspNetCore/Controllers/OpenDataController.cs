using BookingSystem.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using OpenActive.NET;
using OpenActive.Server.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem.AspNetCore.Controllers
{
    [Route("feeds")]
    [ApiController]
    public class OpenDataController : ControllerBase
    {
        /// <summary>
        /// Open Data Feeds
        /// GET feeds/{feedname}
        /// </summary>
        [HttpGet("{feedname}")]
        [Consumes(OpenActiveMediaTypes.RealtimePagedDataExchange.Version1, System.Net.Mime.MediaTypeNames.Application.Json)] 
        public IActionResult GetOpenDataFeed([FromServices] IBookingEngine bookingEngine, string feedName, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                return bookingEngine.GetOpenDataRPDEPageForFeed(feedName, afterTimestamp, afterId, afterChangeNumber).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

    }
}
