using IdentityServer4.Stores;
using Microsoft.Extensions.DependencyInjection;
using OpenActive.FakeDatabase.NET;

namespace IdentityServer
{
    public static class CustomIdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddFakeUserStore(this IIdentityServerBuilder builder, string jsonLdIdBaseUrl)
        {
            // TODO Think GetRequiredService() is an anti-pattern, so change when proper pattern is found
            builder.Services.AddSingleton<IUserRepository>(repo => new UserRepository(jsonLdIdBaseUrl, repo.GetRequiredService<FakeBookingSystem>()));
            builder.AddProfileService<ProfileService>();

            return builder;
        }
    }
}
