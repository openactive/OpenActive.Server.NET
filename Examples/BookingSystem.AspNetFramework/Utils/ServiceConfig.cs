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
            var appSettings = new AppSettings
            {
                BaseUrl = ConfigurationManager.AppSettings["ApplicationHostBaseUrl"],
                FeatureFlags = new FeatureSettings(), // use default values for all features
                Payment = new PaymentSettings {
                    AccountId = ConfigurationManager.AppSettings["AccountId"],
                    PaymentProviderId = ConfigurationManager.AppSettings["PaymentProviderId"]
                }
            };

            config.Formatters.Add(new OpenBookingInputFormatter());

            var services = new ServiceCollection();
            services.AddTransient<DatasetSiteController>();
            services.AddTransient<OpenDataController>();
            services.AddTransient<OpenBookingController>();
            services.AddSingleton<IBookingEngine>(sp => EngineConfig.CreateStoreBookingEngine(appSettings));

            var resolver = new DependencyResolver(services.BuildServiceProvider(true));
            config.DependencyResolver = resolver;
        }
    }
}
