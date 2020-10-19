using System.Web.Http;
using System.Web.Mvc;
using BookingSystem.AspNetFramework.Helpers;

namespace BookingSystem.AspNetFramework
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(ServiceConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            // RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
