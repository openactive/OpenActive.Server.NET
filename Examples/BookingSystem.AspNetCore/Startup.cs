using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenActive.NET.Rpde.Version1;
using Newtonsoft.Json;
using OpenActive.Server.NET;
using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using Newtonsoft.Json.Converters;
using OpenActive.Server.NET.StoreBooking;
using OpenActive.Server.NET.OpenBookingHelper;
using BookingSystem.AspNetCore.Helpers;

namespace BookingSystem.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO: Authentication disabled for now
            //services.AddAuthentication(AzureADB2CDefaults.BearerAuthenticationScheme)
            //    .AddAzureADB2CBearer(options => Configuration.Bind("AzureAdB2C", options));
            services
                .AddMvc()
                .AddMvcOptions(options => options.InputFormatters.Insert(0, new OpenBookingInputFormatter()))
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            //QUESTION: Should all these be configured here? Are we using the pattern correctly?
            //https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-3.0
            string BaseUrl = Configuration["ApplicationHostBaseUrl"] ?? "http://localhost:5000";
            // Configuration for the reference implementation to be used in either mode, for testing purposes.
            // Note that both modes do not need to be supported by in an actual implmentation.
            bool UseSingleSellerMode = Configuration["UseSingleSellerMode"] == "true";
            services.AddSingleton<IBookingEngine>(sp => EngineConfig.CreateStoreBookingEngine(BaseUrl, UseSingleSellerMode));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
