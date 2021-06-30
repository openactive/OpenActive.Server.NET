// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace IdentityServer
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            AppSettings = new AppSettings();
            configuration.Bind(AppSettings);
        }

        public AppSettings AppSettings { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IClientStore, ClientStore>();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Discovery.CustomEntries.Add("registration_endpoint", "~/connect/register");
                })
                .AddInMemoryIdentityResources(Config.Ids)
                .AddInMemoryApiResources(Config.Apis)
                .AddClientStore<ClientStore>()
                .AddFakeUserStore(AppSettings.JsonLdIdBaseUrl)
                .AddPersistedGrantStore<AcmePersistedGrantStore>()
                .AddProfileService<ProfileService>(); //adding a custom profile service

            services.AddControllersWithViews();

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
