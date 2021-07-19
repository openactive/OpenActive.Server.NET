using IdentityServer4.Models;
using System.Collections.Generic;

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
            openactiveIdentity.DisplayName = "The name, URL, and logo of your organisation and booking system";
            openactiveIdentity.Name = "openactive-identity";

            return new List<IdentityResource>
            {
                new IdentityResources.OpenId() {
                     DisplayName = "The unique identifier of your user account"
                },
                // new IdentityResources.Profile(),
                openactiveIdentity
            };
        }
    }
}
