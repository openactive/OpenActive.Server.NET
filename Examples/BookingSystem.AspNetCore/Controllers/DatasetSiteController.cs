using Microsoft.AspNetCore.Mvc;
using OpenActive.Server.NET;
using BookingSystem.AspNetCore.Helpers;

namespace BookingSystem.AspNetCore.Controllers
{
    [Route("openactive")]
    public class DatasetSiteController : Controller
    {
        // GET: /openactive/
        public IActionResult Index([FromServices] IBookingEngine bookingEngine)
        {
            return bookingEngine.RenderDatasetSite().GetContentResult();
        }
    }
}
