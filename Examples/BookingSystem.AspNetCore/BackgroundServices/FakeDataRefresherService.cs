using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenActive.FakeDatabase.NET;
using BookingSystem.AspNetCore.Services;


namespace BookingSystem
{
    /// <summary>
    /// A background task which periodically refreshes the data in the
    /// FakeBookingSystem. This means that past data is deleted and new copies
    /// are created in the future.
    ///
    /// More information on background tasks here:
    /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#implementing-ihostedservice-with-a-custom-hosted-service-class-deriving-from-the-backgroundservice-base-class
    /// </summary>
    public class FakeDataRefresherService : BackgroundService
    {
        private readonly ILogger<FakeDataRefresherService> _logger;
        private readonly AppSettings _settings;
        private readonly FakeBookingSystem _bookingSystem;
        private readonly DataRefresherStatusService _statusService;

        public FakeDataRefresherService(
            AppSettings settings, 
            ILogger<FakeDataRefresherService> logger,
            FakeBookingSystem bookingSystem,
            DataRefresherStatusService statusService)
        {
            _settings = settings;
            _logger = logger;
            _bookingSystem = bookingSystem;
            _statusService = statusService;

            // Indicate that the refresher service is configured to run
            _statusService.SetRefresherConfigured(true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromHours(_settings.DataRefresherIntervalHours);

            stoppingToken.Register(() =>
                _logger.LogInformation($"FakeDataRefresherService background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"FakeDataRefresherService is starting..");
                var (numDeletedOccurrences, numDeletedSlots) = await _bookingSystem
                  .Database
                  .HardDeleteOldSoftDeletedOccurrencesAndSlots();
                _logger.LogInformation($"FakeDataRefresherService hard deleted {numDeletedOccurrences} occurrences and {numDeletedSlots} slots that were previously old and soft-deleted.");

                var (numRefreshedOccurrences, numRefreshedSlots) = await _bookingSystem
                  .Database
                  .SoftDeletePastOpportunitiesAndInsertNewAtEdgeOfWindow();
                _logger.LogInformation($"FakeDataRefresherService, for {numRefreshedOccurrences} old occurrences and {numRefreshedSlots} old slots, inserted new copies into the future and soft-deleted the old ones.");

                _logger.LogInformation($"FakeDataRefresherService is finished");

                // Signal that a cycle has completed
                _statusService.SignalCycleCompletion();

                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}