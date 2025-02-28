using System;
using System.Threading;
using System.Threading.Tasks;

namespace BookingSystem.AspNetCore.Services
{
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