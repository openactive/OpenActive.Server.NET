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
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
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
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostAsync([FromBody] ClientRegistrationModel model)
        {
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
            var key = KeyGenerator.GenerateSecret();

            StringValues headerValues;
            var registrationKey = string.Empty;

            if (Request.Headers.TryGetValue("Authorization", out headerValues))
            {
                registrationKey = headerValues.FirstOrDefault().Substring("Bearer ".Length);
            }

            // update the booking system
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartnerByInitialAccessToken(registrationKey);
            if (bookingPartner == null)
                return Unauthorized("Initial Access Token is not valid, or is expired");

            bookingPartner.Registered = true;
            bookingPartner.ClientSecret = key.Sha256();
            bookingPartner.Name = model.ClientName;
            bookingPartner.ClientProperties = new OpenActive.FakeDatabase.NET.ClientModel
            {
                ClientUri = model.ClientUri,
                LogoUri = model.LogoUri,
                GrantTypes = model.GrantTypes,
                RedirectUris = model.RedirectUris,
                Scope = model.Scope,
            };
            FakeBookingSystem.Database.SaveBookingPartner(bookingPartner);

            // Read the updated client from the database and reflect back in the request
            var client = await _clients.FindClientByIdAsync(bookingPartner.ClientId);
            if (bookingPartner.ClientSecret != client.ClientSecrets?.FirstOrDefault()?.Value)
            {
                return Problem(title: "New client secret not updated in cache", statusCode: 500);
            }
            var response = new ClientRegistrationResponse
            {
                ClientId = client.ClientId,
                ClientSecret = key,
                ClientName = client.ClientName,
                ClientUri = client.ClientUri,
                LogoUri = client.LogoUri,
                GrantTypes = client.AllowedGrantTypes.ToArray(),
                RedirectUris = client.RedirectUris.ToArray(),
                Scope = string.Join(' ', client.AllowedScopes)
            };

            return CreatedAtAction("ClientRegistration", response);
        }
    }

    public class ClientRegistrationModel
    {
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
        [JsonPropertyName(OidcConstants.RegistrationResponse.ClientId)]
        public string ClientId { get; set; }

        [JsonPropertyName(OidcConstants.RegistrationResponse.ClientSecret)]
        public string ClientSecret { get; set; }
    }
}
