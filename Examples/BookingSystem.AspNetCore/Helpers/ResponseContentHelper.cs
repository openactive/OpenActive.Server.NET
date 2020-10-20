namespace BookingSystem.AspNetCore.Helpers
{
    public static class ResponseContentHelper
    {
        public static Microsoft.AspNetCore.Mvc.ContentResult GetContentResult(this OpenActive.Server.NET.OpenBookingHelper.ResponseContent response)
        {
            return new Microsoft.AspNetCore.Mvc.ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = response.Content,
                ContentType = response.ContentType
            };
        }
    }
}
