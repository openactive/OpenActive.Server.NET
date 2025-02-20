using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenActive.FakeDatabase.NET;


namespace BookingSystem
{
    // Background task
    // More information: https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice#implementing-ihostedservice-with-a-custom-hosted-service-class-deriving-from-the-backgroundservice-base-class
    public class FakeDataRefresherService : BackgroundService
    {
        private readonly ILogger<FakeDataRefresherService> _logger;
        private readonly AppSettings _settings;
        private readonly FakeBookingSystem _bookingSystem;

        public FakeDataRefresherService(
            AppSettings settings, 
            ILogger<FakeDataRefresherService> logger,
            FakeBookingSystem bookingSystem)
        {
            _settings = settings;
            _logger = logger;
            _bookingSystem = bookingSystem;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"FakeDataRefresherService is starting..");

            stoppingToken.Register(() =>
                _logger.LogDebug($"FakeDataRefresherService background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                await _bookingSystem.Database.HardDeleteOldSoftDeletedOccurrencesAndSlots();
                _logger.LogDebug($"FakeDataRefresherService hard deleted opportunities that were previously old and soft deleted");

                await _bookingSystem.Database.SoftDeletePastOpportunitiesAndInsertNewAtEdgeOfWindow();
                _logger.LogDebug($"FakeDataRefresherService soft deleted opportunities and inserted new ones at edge of window.");

                _logger.LogDebug($"FakeDataRefresherService is finished");
                await Task.Delay(_settings.DataRefresherInterval, stoppingToken);
            }
        }
    }
}