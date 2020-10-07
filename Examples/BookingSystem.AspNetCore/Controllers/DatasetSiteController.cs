using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenActive.Server.NET;
using BookingSystem.AspNetCore;
using OpenActive.Server.NET.OpenBookingHelper;
using BookingSystem.AspNetCore.Helpers;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BookingSystem.AspNetCore.Controllers
{
    [Route("openactive")]
    public class DatasetSiteController : Controller
    {
        // GET: /openactive/
        public async Task<IActionResult> Index([FromServices] IBookingEngine bookingEngine)
        {
            return (await bookingEngine.RenderDatasetSiteAsync()).GetContentResult();
        }
    }
}
