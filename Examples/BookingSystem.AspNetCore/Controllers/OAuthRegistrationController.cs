using System.Net.Mime;
using BookingSystem.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem.AspNetCore.Controllers
{
    //[Authorize]
    [Route("api/oauth")]
    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    public class OAuthRegistrationController : ControllerBase
    {
        // Note the authentication aspects of this reference implementation are not yet complete, pending feedback on this:
        // https://tutorials.openactive.io/open-booking-sdk/quick-start-guide/storebookingengine/day-8-authentication

        /// <summary>
        /// Dynamic Client Update
        /// GET api/oauth/register/ABCD1234
        /// </summary>
        [HttpPut("register/{clientId}")]
        public ActionResult<ClientRegistrationResponse> DynamicClientUpdate(string clientId, [FromBody] ClientRegistrationModel clientRegistration)
        {
            string authenticationClientId = AuthenticationHelper.GetClientIdFromAuth(Request, User);

            // Only allow clients to update their own configuration
            if (authenticationClientId != clientId || clientRegistration == null || clientRegistration.ClientId != authenticationClientId) return Unauthorized();

            // TODO: Extend this to update the IdentityModel4 Client list, and return errors in the appropriate format
            // More hints available here: https://github.com/IdentityServer/IdentityServer4/issues/1248#issuecomment-430174923
            // And here: http://docs.identityserver.io/en/latest/quickstarts/1_client_credentials.html
            return new ClientRegistrationResponse
            {
                ClientId = clientRegistration.ClientId,
                //ClientSecret = GenerateSecret(32),
                ClientName = clientRegistration.ClientName,
                ClientUri = clientRegistration.ClientUri,
                LogoUri = clientRegistration.LogoUri,
                GrantTypes = clientRegistration.GrantTypes,
                RedirectUris = clientRegistration.RedirectUris,
                Scope = clientRegistration.Scope
            };        
        }
    }
}
