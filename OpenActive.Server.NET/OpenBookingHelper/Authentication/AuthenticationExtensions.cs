using System.Security.Claims;

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
    }
}
