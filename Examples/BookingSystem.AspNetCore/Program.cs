using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using OpenActive.FakeDatabase.NET;

namespace BookingSystem.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Initialising fake database (shared with IdentityServer)
            FakeBookingSystem.Initialise();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
