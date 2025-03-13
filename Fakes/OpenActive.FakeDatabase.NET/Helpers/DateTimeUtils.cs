using System;

namespace OpenActive.FakeDatabase.NET.Helpers
{
    public static class DateTimeUtils
    {
        /// <summary>
        /// Keep adding `intervalDays` to `dateTime` until the result is in the
        /// future.
        /// </summary>
        public static DateTime MoveToNextFutureInterval(DateTime dateTime, int intervalDays)
        {
            if (dateTime > DateTime.Now)
            {
                return dateTime;
            }

            var daysUntilNow = (DateTime.Now - dateTime).Days;
            var additionalDaysNeeded = ((daysUntilNow / intervalDays) + 1) * intervalDays;
            
            return dateTime.AddDays(additionalDaysNeeded);
        }
    }
}