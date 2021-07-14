using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class UserRepository : IUserRepository
    {
        private readonly string _jsonLdIdBaseUrl;

        public UserRepository(string jsonLdIdBaseUrl)
        {
            this._jsonLdIdBaseUrl = jsonLdIdBaseUrl;
        }

        public Task<bool> ValidateCredentials(string username, string password)
        {
            return FakeBookingSystem.FakeDatabase.ValidateSellerUserCredentials(username, password);
        }

        public async Task<UserWithClaims> FindBySubjectId(string subjectId)
        {
            return long.TryParse(subjectId, out var longSubjectId)
                ? GetUserFromSellerUserWithClaims(await FakeBookingSystem.FakeDatabase.GetSellerUserById(longSubjectId))
                : null;
        }

        public async Task<User> FindByUsername(string username)
        {
            return GetUserFromSellerUser(await FakeBookingSystem.FakeDatabase.GetSellerUser(username));
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
