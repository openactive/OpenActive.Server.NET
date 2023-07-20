using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class UserRepository : IUserRepository
    {
        private readonly string _jsonLdIdBaseUrl;
        private readonly FakeBookingSystem _fakeBookingSystem;

        public UserRepository(string jsonLdIdBaseUrl, FakeBookingSystem fakeBookingSystem)
        {
            this._jsonLdIdBaseUrl = jsonLdIdBaseUrl;
            this._fakeBookingSystem = fakeBookingSystem;
        }

        public Task<bool> ValidateCredentials(string username, string password)
        {
            return _fakeBookingSystem.Database.ValidateSellerUserCredentials(username, password);
        }

        public async Task<UserWithClaims> FindBySubjectId(string subjectId)
        {
            return long.TryParse(subjectId, out var longSubjectId)
                ? GetUserFromSellerUserWithClaims(await _fakeBookingSystem.Database.GetSellerUserById(longSubjectId))
                : null;
        }

        public async Task<User> FindByUsername(string username)
        {
            return GetUserFromSellerUser(await _fakeBookingSystem.Database.GetSellerUser(username));
        }

        // TODO: Make this an extension method
        private static void AddClaimIfNotNull(List<Claim> claims, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                claims.Add(new Claim(key, value));
            }
        }

        private UserWithClaims GetUserFromSellerUserWithClaims(SellerUserTable sellerUser)
        {
            if (sellerUser == null) return null;
            var user = new UserWithClaims
            {
                Username = sellerUser.Username,
                SubjectId = sellerUser.Id.ToString(),
                IsActive = true,
                Claims = new List<Claim>()
            };

            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerName", sellerUser.SellerTable.Name);
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerId", _jsonLdIdBaseUrl + "/api/identifiers/sellers/" + sellerUser.SellerTable.Id);
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerUrl", sellerUser.SellerTable.Url);
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerLogo", sellerUser.SellerTable.LogoUrl);
            return user;
        }

        private static User GetUserFromSellerUser(SellerUserTable sellerUser)
        {
            if (sellerUser == null)
                return null;

            return new User
            {
                Username = sellerUser.Username,
                SubjectId = sellerUser.Id.ToString(),
                IsActive = true,
            };
        }

        public async Task<User> FindByExternalProvider(string provider, string providerUserId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<User> AutoProvisionUser(string provider, string providerUserId, List<Claim> list)
        {
            throw new System.NotImplementedException();
        }
    }

    public class User
    {
        public string Username { get; set; }
        public string SubjectId { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserWithClaims : User
    {
        public List<Claim> Claims { get; set; }
    }
}
