using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingSystem.AspNetCore.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenActive.NET;
using OpenActive.NET.Rpde;
using OpenActive.NET.Rpde.Version1;
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
        public async Task<IActionResult> GetOpenDataFeed([FromServices] IBookingEngine bookingEngine, string feedname, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            try
            {
                // Note only a subset of these parameters will be supplied when this endpoints is called
                // They are all provided here for the bookingEngine to choose the correct endpoint
                return (await bookingEngine.GetOpenDataRPDEPageForFeedAsync(feedname, afterTimestamp, afterId, afterChangeNumber)).GetContentResult();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }

    }
}
