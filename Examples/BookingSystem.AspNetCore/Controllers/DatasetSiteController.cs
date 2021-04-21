using Microsoft.AspNetCore.Mvc;
using OpenActive.Server.NET;
using BookingSystem.AspNetCore.Helpers;
using System.Threading.Tasks;

namespace BookingSystem.AspNetCore.Controllers
{
    [Route("openactive")]
    public class DatasetSiteController : Controller
    {
        // GET: /openactive/
        public async Task<IActionResult> Index([FromServices] IBookingEngine bookingEngine)
        {
            return (await bookingEngine.RenderDatasetSite()).GetContentResult();
        }
    }
}
