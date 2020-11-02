using IdentityModel;
using OpenActive.FakeDatabase.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class UserRepository : IUserRepository
    {
        // since user data is being retrieved from an api, this store is built iteratively as authentication requests are made
        // it will contain only a subset of the MWS user store at any one time
        // if the server is restarted the store will be purged
        private readonly List<BookingPartnerAdministratorTable> _users = FakeBookingSystem.Database.BookingPartnerAdministrators;

        public bool ValidateCredentials(string username, string password)
        {
            var user = FindByUsername(username);
            if (user != null)
            {
                return user.Password.Equals(password);
            }

            return false;
        }

        public BookingPartnerAdministratorTable FindBySubjectId(string subjectId)
        {
            return _users.FirstOrDefault(x => x.SubjectId == subjectId);
        }

        public BookingPartnerAdministratorTable FindByUsername(string username)
        {
            return _users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}
