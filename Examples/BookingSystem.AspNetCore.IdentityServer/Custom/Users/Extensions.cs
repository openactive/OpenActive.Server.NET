using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public static class CustomIdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddFakeUserStore(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.AddProfileService<ProfileService>();
            builder.AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

            return builder;
        }
    }
}
