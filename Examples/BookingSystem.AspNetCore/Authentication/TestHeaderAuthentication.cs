using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BookingSystem.AspNetCore.Helpers
{
    /**
    * DO NOT USE THIS FILE IN PRODUCTION. This approach is for testing only, and provides no security whatsoever.
    */

    public class TestHeaderAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "Test Headers";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }

    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddTestHeaderAuthenticationSupport(this AuthenticationBuilder authenticationBuilder, Action<TestHeaderAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<TestHeaderAuthenticationOptions, TestHeaderAuthenticationHandler>(TestHeaderAuthenticationOptions.DefaultScheme, options);
        }
    }

    public class TestHeaderAuthenticationHandler : AuthenticationHandler<TestHeaderAuthenticationOptions>
    {
        public TestHeaderAuthenticationHandler(
            IOptionsMonitor<TestHeaderAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Get the claims from headers if they exist
            Request.Headers.TryGetValue(AuthenticationTestHeaders.ClientId, out var testClientId);
            Request.Headers.TryGetValue(AuthenticationTestHeaders.SellerId, out var testSellerId);
            var clientId = testClientId.FirstOrDefault();
            var sellerId = testSellerId.FirstOrDefault();

            // This just passes the test headers through to the claims - it does not provide any security.
            var claims = new List<Claim>();
            if (clientId != null) claims.Add(new Claim(OpenActiveCustomClaimNames.ClientId, clientId));
            if (sellerId != null) claims.Add(new Claim(OpenActiveCustomClaimNames.SellerId, sellerId));

            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            // No checks are made, so this always succeeds. It's just setting the claims if they exist.
            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {

            Response.StatusCode = 401;
            await Response.WriteAsync(OpenActiveSerializer.Serialize(new InvalidAPITokenError()));
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            await Response.WriteAsync(OpenActiveSerializer.Serialize(new UnauthenticatedError()));
        }
    }
}
