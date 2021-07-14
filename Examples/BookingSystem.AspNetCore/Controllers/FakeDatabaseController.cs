using System;
using System.Threading.Tasks;
using BookingSystem.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using OpenActive.NET;
using OpenActive.Server.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.FakeDatabase.NET;
using System.Net;

namespace BookingSystem.AspNetCore.Controllers
{
    [Route("api/fake-database")]
    [ApiController]
    [Consumes(OpenActiveMediaTypes.OpenBooking.Version1)]
    public class FakeDatabaseController : ControllerBase
    {
        /// <summary>
        /// Refreshes the data in the fake database.
        /// Soft deleted data older than a day is hard deleted.
        /// Data now in the past is soft deleted
        /// POST api/fake-database/refresh-data
        /// </summary>
        [HttpPost("fake-database/refresh-data")]
        public async Task<IActionResult> RefreshData([FromServices] IBookingEngine bookingEngine)
        {
            try
            {
                await FakeBookingSystem.FakeDatabase.HardDeletedOldSoftDeletedOccurrencesAndSlots();
                await FakeBookingSystem.FakeDatabase.SoftDeletedPastOccurrencesAndSlots();
                //await FakeBookingSystem.FakeDatabase.CreateOccurrencesAndSlotsAtEndOfWindow();

                return NoContent();
            }
            catch (OpenBookingException obe)
            {
                return obe.ErrorResponseContent.GetContentResult();
            }
        }
    }
}

