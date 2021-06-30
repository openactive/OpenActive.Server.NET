using System;
using System.Security.Claims;
using OpenActive.NET;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Gets the "sub" claim from the JWT
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetSub(this ClaimsPrincipal principal)
        {
            // Note the Sub claim is mapped to NameIdentifier in .NET for legacy reasons
            // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/415
            // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/rel/5.5.0/src/System.IdentityModel.Tokens.Jwt/ClaimTypeMapping.cs#L59
            return principal?.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the ClientId custom claim from the JWT
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetClientId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type == OpenActiveCustomClaimNames.ClientId)?.Value;
        }

        /// <summary>
        /// Gets the SellerId custom claim from the JWT
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetSellerId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(x => x.Type == OpenActiveCustomClaimNames.SellerId)?.Value;
        }

        /// <summary>
        /// Gets the SellerId and ClientId custom claims from the JWT
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static (string clientId, Uri sellerId) GetAccessTokenOpenBookingClaims(this ClaimsPrincipal principal)
        {
            var clientId = principal.GetClientId();
            var sellerId = principal.GetSellerId().ParseUrlOrNull();
            if (clientId != null && sellerId != null)
            {
                return (clientId, sellerId);
            }
            else
            {
                throw new OpenBookingException(new InvalidAPITokenError());
            }
        }

        /// <summary>
        /// Gets the ClientId custom claim from the JWT
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        public static string GetAccessTokenOrdersFeedClaim(this ClaimsPrincipal principal)
        {
            var clientId = principal.GetClientId();
            if (clientId != null)
            {
                return clientId;
            }
            else
            {
                throw new OpenBookingException(new InvalidAPITokenError());
            }
        }

    }
}
