using System.Threading.Tasks;

namespace IdentityServer
{
    public interface IUserRepository
    {
        Task<bool> ValidateCredentials(string username, string password);
        Task<UserWithClaims> FindBySubjectId(string subjectId);
        Task<User> FindByUsername(string username);
    }
}
