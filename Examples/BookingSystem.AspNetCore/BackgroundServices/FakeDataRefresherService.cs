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
                await Task.Delay(_settings.DataRefresherInterval, stoppingToken);
            }
        }
    }
}