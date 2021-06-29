
using BookingSystem.AspNetFramework.Helpers;
using OpenActive.Server.NET;
using System.Net.Http;
using System.Threading.Tasks;
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
        public async Task<HttpResponseMessage> Index()
        {
            return (await _bookingEngine.RenderDatasetSite()).GetContentResult();
        }
    }
}
