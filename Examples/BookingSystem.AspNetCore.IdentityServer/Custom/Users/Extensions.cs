using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer
{
    public static class CustomIdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddFakeUserStore(this IIdentityServerBuilder builder, string jsonLdIdBaseUrl)
        {
            builder.Services.AddSingleton<IUserRepository>(repo => new UserRepository(jsonLdIdBaseUrl));
            builder.AddProfileService<ProfileService>();
            builder.AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

            return builder;
        }
    }
}
