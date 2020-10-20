using BookingSystem.AspNetFramework.Helpers;
using BookingSystem.AspNetFramework.Controllers;
using Microsoft.Extensions.DependencyInjection;
using OpenActive.Server.NET;
using System.Configuration;
using System.Web.Http;
using BookingSystem.AspNetFramework.Utils;

namespace BookingSystem.AspNetFramework
{
    public class ServiceConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Formatters.Add(new OpenBookingInputFormatter());

            var baseUrl = ConfigurationManager.AppSettings["ApplicationHostBaseUrl"] ?? "https://localhost:5001";
            var useSingleSellerMode = ConfigurationManager.AppSettings["UseSingleSellerMode"] == "true";

            var services = new ServiceCollection();
            services.AddTransient<DatasetSiteController>();
            services.AddTransient<OpenDataController>();
            services.AddTransient<OpenBookingController>();
            services.AddSingleton<IBookingEngine>(sp => EngineConfig.CreateStoreBookingEngine(baseUrl, useSingleSellerMode));

            var resolver = new DependencyResolver(services.BuildServiceProvider(true));
            config.DependencyResolver = resolver;
        }
    }
}
