using OpenActive.FakeDatabase.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public interface IUserRepository
    {
        bool ValidateCredentials(string username, string password);
        UserWithClaims FindBySubjectId(string subjectId);
        User FindByUsername(string username);
    }
}
