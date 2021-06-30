using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;

namespace BookingSystem.AspNetFramework.Helpers
{
    public static class AuthenticationHelper
    {
        private static (string clientId, Uri sellerId) GetIdsFromAuth(HttpRequestMessage request, IPrincipal principal, bool requireSellerId)
        {
            // NOT FOR PRODUCTION USE: Please remove this block in production
            IEnumerable<string> testSellerId = null;
            if (request.Headers.TryGetValues(AuthenticationTestHeaders.ClientId, out IEnumerable<string> testClientId)
                && testClientId.Count() == 1
                && (!requireSellerId || (request.Headers.TryGetValues(AuthenticationTestHeaders.SellerId, out testSellerId) && testSellerId.FirstOrDefault().ParseUrlOrNull() != null))
                )
            {
                return (testClientId.FirstOrDefault(), testSellerId?.FirstOrDefault().ParseUrlOrNull());
            }

            // For production use: Get Ids from JWT
            var claimsPrincipal = principal as ClaimsPrincipal;
            var clientId = claimsPrincipal.GetClientId();
            var sellerId = claimsPrincipal.GetSellerId().ParseUrlOrNull();
            if (clientId != null && (sellerId != null || !requireSellerId))
            {
                return (clientId, sellerId);
            }
            else
            {
                throw new OpenBookingException(new InvalidAPITokenError());
            }
        }

        public static (string clientId, Uri sellerId) GetIdsFromAuth(HttpRequestMessage request, IPrincipal principal)
        {
            return GetIdsFromAuth(request, principal, true);
        }

        public static string GetClientIdFromAuth(HttpRequestMessage request, IPrincipal principal)
        {
            return GetIdsFromAuth(request, principal, false).clientId;
        }
    }
}
