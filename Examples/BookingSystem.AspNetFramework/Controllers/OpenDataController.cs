using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using BookingSystem.AspNetFramework.Helpers;
using OpenActive.Server.NET;

namespace BookingSystem.AspNetFramework.Controllers
{
    public class OpenDataController : ApiController
    {
        private IBookingEngine _bookingEngine;

        public OpenDataController(IBookingEngine bookingEngine)
        {
            _bookingEngine = bookingEngine;
        }

        /// <summary>
        /// Open Data Feeds
        /// GET feeds/{feedname}
        /// </summary>
        [HttpGet]
        [Route("feeds/{feedname}")]
        // [Consumes(MediaTypeNames.RealtimePagedDataExchange.Version1, System.Net.Mime.MediaTypeNames.Application.Json)] 
        public HttpResponseMessage GetOpenDataFeed(string feedname, long? afterTimestamp = (long?)null, string afterId = null, long? afterChangeNumber = (long?)null)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                return _bookingEngine.GetOpenDataRpdePageForFeed(feedname, afterTimestamp, afterId, afterChangeNumber).GetContentResult();
            }
            catch (KeyNotFoundException kn)
            {
                return Request.CreateResponse(System.Net.HttpStatusCode.NotFound);
            }
        }

    }
}
