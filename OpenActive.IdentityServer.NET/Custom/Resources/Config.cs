// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            CustomIdentityResource.GetIdentityResources();

        public static IEnumerable<ApiResource> Apis =>
           new List<ApiResource>
           {
                new ApiResource
                {
                    Name = "openbooking",
                    ApiSecrets =
                    {
                        new Secret("secret".Sha256())
                    },
                    Scopes =
                    {
                        new Scope()
                        {
                            Name = "openactive-openbooking",
                            DisplayName = "Access to C1, C2, B Endpoints",
                            UserClaims = { JwtClaimTypes.Name, "https://openactive.io/sellerId", "https://openactive.io/clientId" }
                        },
                        new Scope
                        {
                            Name = "openactive-ordersfeed",
                            DisplayName = "Access to Orders RPDE Feeds",
                            UserClaims = { JwtClaimTypes.Name, "https://openactive.io/clientId" }
                        },
                        new Scope
                        {
                            Name = "oauth-dymamic-client-update",
                            DisplayName = "Access to perform a Dynamic Client Update",
                            UserClaims = { JwtClaimTypes.Name, "https://openactive.io/clientId" }
                        }
                    }
                }
           };
    }
}