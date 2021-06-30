using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class ProfileService : IProfileService
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The users
        /// </summary>
        protected readonly IUserRepository Users;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileService"/> class.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="logger">The logger.</param>
        public ProfileService(IUserRepository users, ILogger<ProfileService> logger)
        {
            Users = users;
            Logger = logger;
        }

        /// <summary>
        /// This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo endpoint)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.LogProfileRequest(Logger);

            // Claims added here are defined be the API and RESOURCE configurations in Config.cs, only the requested claims will be added to the IssuedClaims collection
            if (context.RequestedClaimTypes.Any())
            {
                var user = await Users.FindBySubjectId(context.Subject.GetSubjectId());
                if (user != null)
                {
                    context.AddRequestedClaims(user.Claims);
                    // Add the sellerId claim if it was not already added, to ensure it appears in the access_token
                    var sellerIdClaim = user.Claims.FirstOrDefault(x => x.Type == "https://openactive.io/sellerId");
                    if (sellerIdClaim != null && !context.IssuedClaims.Exists(x => x.Type == "https://openactive.io/sellerId"))
                    {
                        context.IssuedClaims.Add(sellerIdClaim);
                    }
                }
            }

            context.LogIssuedClaims(Logger);
        }

        /// <summary>
        /// This method gets called whenever identity server needs to determine if the user is valid or active (e.g. if the user's account has been deactivated since they logged in).
        /// (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public virtual async Task IsActiveAsync(IsActiveContext context)
        {
            Logger.LogDebug("IsActive called from: {Caller}", context.Caller);

            var user = await Users.FindBySubjectId(context.Subject.GetSubjectId());
            context.IsActive = user?.IsActive == true;
        }
    }
}
