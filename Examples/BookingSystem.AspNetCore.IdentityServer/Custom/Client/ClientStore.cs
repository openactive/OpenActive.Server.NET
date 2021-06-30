using IdentityServer4.Models;
using IdentityServer4.Stores;
using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class ClientStore : IClientStore
    {
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var bookingPartner = await FakeBookingSystem.Database.GetBookingPartner(clientId);
            return ConvertToIs4Client(bookingPartner);
        }

        private static Client ConvertToIs4Client(BookingPartnerTable bookingPartner)
        {
            if (bookingPartner == null)
                return null;

            return new Client
            {
                Enabled = bookingPartner.Registered,
                ClientId = bookingPartner.ClientId,
                ClientName = bookingPartner.Name,
                AllowedGrantTypes = bookingPartner.GrantTypes == null ? new List<string>() : bookingPartner.GrantTypes.ToList(),
                ClientSecrets = bookingPartner.ClientSecret == null ? new List<Secret>() : new List<Secret> { new Secret(bookingPartner.ClientSecret) },
                AllowedScopes = bookingPartner.Scope == null ? new List<string>() : bookingPartner.Scope.Split(' ').ToList(),
                Claims = bookingPartner.ClientId == null ? new List<System.Security.Claims.Claim>() : new List<System.Security.Claims.Claim>() { new System.Security.Claims.Claim("https://openactive.io/clientId", bookingPartner.ClientId) },
                ClientClaimsPrefix = "",
                AlwaysSendClientClaims = true,
                AlwaysIncludeUserClaimsInIdToken = true,
                AllowOfflineAccess = true,
                UpdateAccessTokenClaimsOnRefresh = true,
                RedirectUris = bookingPartner.RedirectUris == null ? new List<string>() : bookingPartner.RedirectUris.ToList(),
                RequireConsent = true,
                RequirePkce = true,
                LogoUri = bookingPartner.LogoUri,
                ClientUri = bookingPartner.ClientUri
            };
        }
    }
}