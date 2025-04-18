﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenActive.Server.NET;
using BookingSystem.AspNetCore.Helpers;
using System.Net.Http;
using OpenActive.Server.NET.OpenBookingHelper;
using Microsoft.AspNetCore.Authorization;
using OpenActive.FakeDatabase.NET;
using Microsoft.Extensions.Logging;
using BookingSystem.AspNetCore.Services;

namespace BookingSystem.AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            AppSettings = new AppSettings();
            configuration.Bind(AppSettings);

            // Provide a simple way to disable token auth for some testing scenarios
            var disableTokenAuthEnvVar = System.Environment.GetEnvironmentVariable("DISABLE_TOKEN_AUTH");
            if (disableTokenAuthEnvVar == "true")
            {
                AppSettings.FeatureFlags.EnableTokenAuth = false;
            }
            else if (disableTokenAuthEnvVar == "false")
            {
                AppSettings.FeatureFlags.EnableTokenAuth = true;
            }

            // Provide a simple way to enable FacilityUseHasSlots for some testing scenarios
            var facilityUseHasSlotsEnvVar = System.Environment.GetEnvironmentVariable("FACILITY_USE_HAS_SLOTS");
            if (facilityUseHasSlotsEnvVar == "true")
            {
                AppSettings.FeatureFlags.FacilityUseHasSlots = true;
            }
            else if (facilityUseHasSlotsEnvVar == "false")
            {
                AppSettings.FeatureFlags.FacilityUseHasSlots = false;
            }

            // Provide a simple way to enable CI mode 
            var isLoremFitsumModeEnvVar = System.Environment.GetEnvironmentVariable("IS_LOREM_FITSUM_MODE");
            if (isLoremFitsumModeEnvVar == "true")
            {
                AppSettings.FeatureFlags.IsLoremFitsumMode = true;
            }
            else if (isLoremFitsumModeEnvVar == "false")
            {
                AppSettings.FeatureFlags.IsLoremFitsumMode = false;
            }
        }

        public AppSettings AppSettings { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (AppSettings.FeatureFlags.EnableTokenAuth)
            {
                services.AddAuthentication("Bearer")
                    .AddJwtBearer("Bearer", options =>
                    {
                        options.Authority = AppSettings.OpenIdIssuerUrl;
                        options.Audience = "openbooking";
                        // Note these two options must be removed for a production implementation - they force TLS certificate validation to be ignored
                        options.RequireHttpsMetadata = false;
                        options.BackchannelHttpHandler = new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                        };
                    });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy(OpenActiveScopes.OpenBooking, policy => policy.Requirements.Add(new HasScopeRequirement(OpenActiveScopes.OpenBooking, AppSettings.OpenIdIssuerUrl)));
                    options.AddPolicy(OpenActiveScopes.OrdersFeed, policy => policy.Requirements.Add(new HasScopeRequirement(OpenActiveScopes.OrdersFeed, AppSettings.OpenIdIssuerUrl)));
                });
                services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
            }
            else
            {
                // DO NOT USE THIS IN PRODUCTION.
                // This passes test API headers straight through to claims, and provides no security whatsoever.
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestHeaderAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = TestHeaderAuthenticationOptions.DefaultScheme;

                })
                .AddTestHeaderAuthenticationSupport(options => { });
                services.AddAuthorization(options =>
                {
                    // No authorization checks are performed, this just ensures that the required claims are supplied
                    options.AddPolicy(OpenActiveScopes.OpenBooking, policy =>
                    {
                        policy.RequireClaim(OpenActiveCustomClaimNames.ClientId);
                        policy.RequireClaim(OpenActiveCustomClaimNames.SellerId);
                    });
                    options.AddPolicy(OpenActiveScopes.OrdersFeed, policy => policy.RequireClaim(OpenActiveCustomClaimNames.ClientId));
                });
            }

            services
                .AddControllers()
                .AddMvcOptions(options => options.InputFormatters.Insert(0, new OpenBookingInputFormatter()));

            services.AddSingleton(x => AppSettings);

            services.AddSingleton(sp => new FakeBookingSystem
            (
                AppSettings.FeatureFlags.FacilityUseHasSlots,
                sp.GetRequiredService<ILogger<FakeBookingSystem>>()
            ));

            services.AddSingleton<DataRefresherStatusService>();

            services.AddSingleton<IBookingEngine>(sp => 
                EngineConfig.CreateStoreBookingEngine(
                    AppSettings, 
                    sp.GetRequiredService<FakeBookingSystem>()
                ));

            var doRunDataRefresher = Environment.GetEnvironmentVariable("PERIODICALLY_REFRESH_DATA")?.ToLowerInvariant() == "true";
            if (doRunDataRefresher)
            {
                services.AddHostedService<FakeDataRefresherService>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, DataRefresherStatusService statusService)
        {
            // If data refresher is not configured to run, make this clear in the status service
            var doRunDataRefresher = Environment.GetEnvironmentVariable("PERIODICALLY_REFRESH_DATA")?.ToLowerInvariant() == "true";
            if (!doRunDataRefresher)
            {
                statusService.SetRefresherConfigured(false);
            }

            // Note this will prevent UnknownOrIncorrectEndpointError being produced for 404 status in Development mode
            // Hence the `unknown-endpoint` test of the OpenActive Test Suite will always fail in Development mode
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/api/openbooking/error/{0}");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
