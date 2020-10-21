
using BookingSystem.AspNetFramework.Helpers;
using OpenActive.DatasetSite.NET;
using OpenActive.Server.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
// using System.Web.Mvc;

namespace BookingSystem.AspNetFramework.Controllers
{

    [Route("openactive")]
    public class DatasetSiteController : ApiController
    {
        private IBookingEngine _bookingEngine = null;

        public DatasetSiteController(IBookingEngine bookingEngine)
        {
            _bookingEngine = bookingEngine;
        }

        // GET: /openactive/
        [HttpGet]
        public HttpResponseMessage Index()
        {
            return _bookingEngine.RenderDatasetSite().GetContentResult();
        }
    }
}
