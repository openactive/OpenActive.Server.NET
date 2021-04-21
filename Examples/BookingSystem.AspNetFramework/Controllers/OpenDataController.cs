using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using BookingSystem.AspNetFramework.Helpers;
using OpenActive.NET;
using OpenActive.Server.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem.AspNetFramework.Controllers
{
    public class OpenDataController : ApiController
    {
        private IBookingEngine _bookingEngine = null;

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
        public async Task<HttpResponseMessage> GetOpenDataFeed(string feedname, long? afterTimestamp = (long?)null, string afterId = null, long? afterChangeNumber = (long?)null)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                return (await _bookingEngine.GetOpenDataRPDEPageForFeed(feedname, afterTimestamp, afterId, afterChangeNumber)).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }
    }
}
