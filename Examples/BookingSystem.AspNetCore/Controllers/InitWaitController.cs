using System;
using System.Threading.Tasks;
using BookingSystem.AspNetCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// Endpoints which are used to wait for components within
/// BookingSystem.AspNetCore to be initialized.
/// </summary>
namespace BookingSystem.AspNetCore.Controllers
{
    [ApiController]
    [Route("init-wait")]
    public class InitWaitController : ControllerBase
    {
        private readonly DataRefresherStatusService _statusService;
        private readonly ILogger<InitWaitController> _logger;
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(5);

        public InitWaitController(
            DataRefresherStatusService statusService,
            ILogger<InitWaitController> logger)
        {
            _statusService = statusService;
            _logger = logger;
        }

        /// <summary>
        /// Wait for the data refresher to complete its first cycle.
        /// Returns 204 when the data refresher has completed its first cycle.
        /// Returns 503 if the data refresher is not configured to run.
        /// Returns 504 if the data refresher fails to complete a cycle within the default timeout.
        /// </summary>
        [HttpGet("data-refresher")]
        public async Task<IActionResult> WaitForDataRefresher()
        {
            _logger.LogDebug("Received request to wait for data refresher completion");

            // Check if the data refresher is configured to run
            if (!_statusService.IsRefresherConfigured())
            {
                _logger.LogWarning("Data refresher is not configured to run");
                return StatusCode(503, "Data refresher service is not configured to run");
            }

            // If it has already completed a cycle, return immediately
            if (_statusService.HasCompletedCycle())
            {
                _logger.LogDebug("Data refresher has already completed a cycle");
                return NoContent();
            }

            _logger.LogDebug("Waiting for data refresher to complete a cycle...");
            
            // Wait for the cycle to complete, with a timeout
            await _statusService.WaitForCycleCompletion(_defaultTimeout);
            
            if (_statusService.HasCompletedCycle())
            {
                _logger.LogDebug("Data refresher completed a cycle, returning 204");
                return NoContent();
            }
            else
            {
                _logger.LogWarning("Timed out waiting for data refresher to complete a cycle");
                return StatusCode(504, "Timed out waiting for data refresher to complete a cycle");
            }
        }
    }
} 