using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class CustomIdentityResource
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            var openactiveIdentity = new IdentityResource();
            openactiveIdentity.UserClaims = new[] {
                    "https://openactive.io/sellerId",
                    "https://openactive.io/sellerName",
                    "https://openactive.io/sellerUrl",
                    "https://openactive.io/sellerLogo",
                    "https://openactive.io/bookingServiceName",
                    "https://openactive.io/bookingServiceUrl",};
            openactiveIdentity.Required = true;
            openactiveIdentity.DisplayName = "Access to Openactive Identity claims about you, the Seller.";
            openactiveIdentity.Name = "openactive-identity";

            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                openactiveIdentity
            };
        }
    }
}
