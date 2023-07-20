using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class ClientStore : IClientStore
    {
        private readonly FakeBookingSystem _fakeBookingSystem;

        public ClientStore(FakeBookingSystem fakeBookingSystem)
        {
            _fakeBookingSystem = fakeBookingSystem;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var bookingPartner = await _fakeBookingSystem.Database.BookingPartnerGetByClientId(clientId);
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
                Claims = bookingPartner.ClientId == null ? new List<ClientClaim>() : new List<ClientClaim>() { new ClientClaim("https://openactive.io/clientId", bookingPartner.ClientId) },
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