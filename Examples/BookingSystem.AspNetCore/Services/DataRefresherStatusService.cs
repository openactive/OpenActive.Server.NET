using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookingSystem.AspNetCore.Services
{
    /// <summary>
    /// A service which tracks the status of the data refresher, including
    /// whether it is configured to run and whether a cycle has completed.
    /// </summary>
    public class DataRefresherStatusService
    {
        private readonly SemaphoreSlim _completionSemaphore = new SemaphoreSlim(0, 1);
        private bool _isRefresherConfigured = false;
        private bool _hasCompletedCycle = false;

        public void SetRefresherConfigured(bool isConfigured)
        {
            _isRefresherConfigured = isConfigured;
        }

        public bool IsRefresherConfigured()
        {
            return _isRefresherConfigured;
        }

        public void SignalCycleCompletion()
        {
            _hasCompletedCycle = true;
            
            // Release the semaphore if someone is waiting on it
            if (_completionSemaphore.CurrentCount == 0)
            {
                _completionSemaphore.Release();
            }
        }

        /// <summary>
        /// Has the data refresher completed a cycle?
        ///
        /// This makes it possible to write scripts (for CI) which don't start
        /// until the data refresher has completed at least one cycle.
        /// </summary>
        public bool HasCompletedCycle()
        {
            return _hasCompletedCycle;
        }

        public async Task WaitForCycleCompletion(TimeSpan timeout)
        {
            if (_hasCompletedCycle)
            {
                return;
            }

            await _completionSemaphore.WaitAsync(timeout);
        }
    }
} 