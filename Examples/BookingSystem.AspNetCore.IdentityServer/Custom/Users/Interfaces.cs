using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public interface IUserRepository
    {
        Task<bool> ValidateCredentials(string username, string password);
        Task<UserWithClaims> FindBySubjectId(string subjectId);
        Task<User> FindByUsername(string username);
        Task<User> FindByExternalProvider(string provider, string providerUserId);
        Task<User> AutoProvisionUser(string provider, string providerUserId, List<Claim> list);
    }
}
