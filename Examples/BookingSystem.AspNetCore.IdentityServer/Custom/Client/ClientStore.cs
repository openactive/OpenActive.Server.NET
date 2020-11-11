using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using OpenActive.FakeDatabase.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class ClientStore : IClientStore
    {
        public Task<Client> FindClientByIdAsync(string clientId)
        {
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(clientId);
            return Task.FromResult(this.ConvertToIS4Client(bookingPartner));
        }

        private Client ConvertToIS4Client(BookingPartnerTable bookingPartner)
        {
            if (bookingPartner == null) return null;
            if (bookingPartner.ClientJson.GrantTypes.Contains("authorization_code"))
            {
                return new Client()
                {
                    ClientId = bookingPartner.ClientId,
                    ClientName = bookingPartner.ClientJson.ClientName,
                    AllowedGrantTypes = bookingPartner.ClientJson.GrantTypes.ToList(),
                    ClientSecrets = { new Secret(bookingPartner.ClientSecret.Sha256()) },
                    AllowedScopes = bookingPartner.ClientJson.Scope.Split(' ').ToList(),
                    Claims = new List<System.Security.Claims.Claim>() { new System.Security.Claims.Claim("https://openactive.io/clientId", bookingPartner.ClientId) },
                    ClientClaimsPrefix = "",
                    AlwaysSendClientClaims = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowOfflineAccess = true,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    RedirectUris = bookingPartner.ClientJson.RedirectUris.ToList(),
                    RequireConsent = true,
                    RequirePkce = true, 
                };
            } else
            {
                return new Client()
                {
                    Enabled = bookingPartner.Registered,
                    ClientId = bookingPartner.ClientId,
                    ClientName = bookingPartner.ClientJson.ClientName,
                    AllowedGrantTypes = bookingPartner.ClientJson.GrantTypes.ToList(),
                    ClientSecrets = { new Secret(bookingPartner.ClientSecret.Sha256()) },
                    AllowedScopes = bookingPartner.ClientJson.Scope.Split(' ').ToList(),
                    Claims = new List<System.Security.Claims.Claim>() { new System.Security.Claims.Claim("https://openactive.io/clientId", bookingPartner.ClientId) },
                    ClientClaimsPrefix = "",
                    AlwaysSendClientClaims = true,
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowOfflineAccess = true,
                    UpdateAccessTokenClaimsOnRefresh = true
                };
            }
            
        }
    }
}