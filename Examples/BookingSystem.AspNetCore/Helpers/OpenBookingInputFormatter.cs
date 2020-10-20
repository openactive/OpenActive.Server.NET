using Microsoft.AspNetCore.Mvc.Formatters;
using OpenActive.NET;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BookingSystem.AspNetCore.Helpers
{
    public class OpenBookingInputFormatter : InputFormatter
    {
        public OpenBookingInputFormatter()
        {
            SupportedMediaTypes.Add(OpenActiveMediaTypes.OpenBooking.Version1);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();
                return await InputFormatterResult.SuccessAsync(content);
            }
        }

        protected override bool CanReadType(Type type)
        {
            return type == typeof(string);
        }
    }
}
