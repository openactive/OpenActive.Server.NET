using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using OpenActive.FakeDatabase.NET;

namespace BookingSystem.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            FakeBookingSystem.Initialise();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .CaptureStartupErrors(true)
            .UseSetting("detailedErrors", "true")
            .UseStartup<Startup>();
    }
}
