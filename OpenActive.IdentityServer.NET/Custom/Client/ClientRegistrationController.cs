using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using OpenActive.FakeDatabase.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IdentityServer
{
    [Route("connect/register")]
    [ApiController]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class ClientResistrationController : ControllerBase
    {
        private readonly IClientStore _clients;

        public ClientResistrationController(IClientStore clients)
        {
            _clients = clients;
        }

        // POST: connect/register
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostAsync([FromBody] ClientRegistrationModel model)
        {
            if (!Request.IsHttps)
            {
                return BadRequest("HTTPS is required at this endpoint.");
            }

            if (model.GrantTypes == null)
            {
                model.GrantTypes = new[] { OidcConstants.GrantTypes.AuthorizationCode, OidcConstants.GrantTypes.RefreshToken };
            }

            if (model.GrantTypes.Any(x => x == OidcConstants.GrantTypes.Implicit) || model.GrantTypes.Any(x => x == OidcConstants.GrantTypes.AuthorizationCode))
            {
                if (!model.RedirectUris.Any())
                {
                    return BadRequest("A redirect URI is required for the supplied grant type.");
                }

                if (model.RedirectUris.Any(redirectUri => !Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute)))
                {
                    return BadRequest("One or more of the redirect URIs are invalid.");
                }
            }

            // generate a secret for the client
            var hmac = new HMACSHA256();
            var key = Convert.ToBase64String(hmac.Key);

            StringValues headerValues;
            var registrationKey = string.Empty;

            if (Request.Headers.TryGetValue("Authorization", out headerValues))
            {
                registrationKey = headerValues.FirstOrDefault();
            }

            // update the booking system
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(model.ClientId);
            if (bookingPartner == null)
                return NotFound("Client was not found");
            if (bookingPartner.RegistrationKey != registrationKey || bookingPartner.RegistrationKeyValidUntil > DateTime.Now)
                return Unauthorized("Registration key is not valid, or is expired");

            bookingPartner.Registered = true;
            bookingPartner.ClientJson.ClientName = model.ClientName;
            bookingPartner.ClientJson.ClientUri = model.ClientUri;
            bookingPartner.ClientJson.LogoUri = model.LogoUri;
            bookingPartner.ClientJson.GrantTypes = model.GrantTypes;
            bookingPartner.ClientJson.Scope = model.Scope;
            bookingPartner.ClientSecret = key;
            FakeBookingSystem.Database.SaveBookingPartner(bookingPartner);

            var client = await _clients.FindClientByIdAsync(model.ClientId);
            client.Enabled = true;
            client.ClientName = model.ClientName;
            client.ClientUri = model.ClientUri;
            client.LogoUri = model.LogoUri;
            client.AllowedGrantTypes = model.GrantTypes.ToList();
            client.AllowedScopes = model.Scope.Split(' ').ToList();
            client.ClientSecrets = new List<Secret>() { new Secret(key.Sha256()) };

            var response = new ClientRegistrationResponse
            {
                ClientId = bookingPartner.ClientId,
                ClientSecret = key,
                ClientName = model.ClientName,
                ClientUri = model.ClientUri,
                LogoUri = model.LogoUri,
                GrantTypes = model.GrantTypes,
                RedirectUris = model.RedirectUris,
                Scope = model.Scope
            };

            return Ok(response);
        }
    }

    public class ClientRegistrationModel
    {
        [JsonPropertyName(OidcConstants.RegistrationResponse.ClientId)]
        public string ClientId { get; set; }

        [JsonPropertyName(OidcConstants.ClientMetadata.ClientName)]
        public string ClientName { get; set; }

        [JsonPropertyName(OidcConstants.ClientMetadata.ClientUri)]
        public string ClientUri { get; set; }

        [JsonPropertyName(OidcConstants.ClientMetadata.LogoUri)]
        public string LogoUri { get; set; }

        [JsonPropertyName(OidcConstants.ClientMetadata.GrantTypes)]
        public string[] GrantTypes { get; set; }

        [JsonPropertyName(OidcConstants.ClientMetadata.RedirectUris)]
        public string[] RedirectUris { get; set; } = new string[] {};

        public string Scope { get; set; } = "openid profile email";
    }

    public class ClientRegistrationResponse : ClientRegistrationModel
    {
        [JsonPropertyName(OidcConstants.RegistrationResponse.ClientSecret)]
        public string ClientSecret { get; set; }
    }
}
