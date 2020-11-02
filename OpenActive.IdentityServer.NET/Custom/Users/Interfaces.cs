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
        BookingPartnerAdministratorTable FindBySubjectId(string subjectId);
        BookingPartnerAdministratorTable FindByUsername(string username);
    }
}
