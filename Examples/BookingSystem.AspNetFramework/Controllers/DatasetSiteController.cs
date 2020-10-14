
using BookingSystem.AspNetFramework.Helpers;
using OpenActive.Server.NET;
using System.Net.Http;
using System.Web.Http;
// using System.Web.Mvc;

namespace BookingSystem.AspNetFramework.Controllers
{

    [Route("openactive")]
    public class DatasetSiteController : ApiController
    {
        private IBookingEngine _bookingEngine;

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
