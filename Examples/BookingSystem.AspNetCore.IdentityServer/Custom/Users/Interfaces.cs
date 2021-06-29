namespace IdentityServer
{
    public interface IUserRepository
    {
        bool ValidateCredentials(string username, string password);
        UserWithClaims FindBySubjectId(string subjectId);
        User FindByUsername(string username);
    }
}
