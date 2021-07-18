// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            CustomIdentityResource.GetIdentityResources();

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope(name: "openactive-openbooking", displayName: "Access to the Open Booking API", userClaims: new List<string> { JwtClaimTypes.Name, "https://openactive.io/sellerId", "https://openactive.io/clientId" })
                {
                    Required = true,
                },
                new ApiScope(name: "openactive-ordersfeed", displayName: "Access to Orders RPDE Feeds", userClaims: new List<string> { JwtClaimTypes.Name, "https://openactive.io/clientId" })
                {
                    Required = true,
                }
            };

        public static IEnumerable<ApiResource> ApiResources =>
           new List<ApiResource>
           {
                new ApiResource("openbooking", "Open Booking API")
                {
                    ApiSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    Scopes = { "openactive-openbooking", "openactive-ordersfeed" }
                }
           };
    }
}