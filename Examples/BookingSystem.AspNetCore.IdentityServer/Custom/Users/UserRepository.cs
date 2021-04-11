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
        private string jsonLdIdBaseUrl;

        public UserRepository(string jsonLdIdBaseUrl)
        {
            this.jsonLdIdBaseUrl = jsonLdIdBaseUrl;
        }

        public bool ValidateCredentials(string username, string password)
        {
            return FakeBookingSystem.Database.ValidateSellerUserCredentials(username, password);
        }

        public UserWithClaims FindBySubjectId(string subjectId)
        {
            if (long.TryParse(subjectId, out long longSubjectId))
            {
                return GetUserFromSellerUserWithClaims(FakeBookingSystem.Database.GetSellerUserById(longSubjectId));
            }
            else
            {
                return null;
            }
        }

        public User FindByUsername(string username)
        {
            return GetUserFromSellerUser(FakeBookingSystem.Database.GetSellerUser(username));
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
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerId", jsonLdIdBaseUrl + "/api/identifiers/sellers/" + sellerUser.SellerTable.Id);
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerUrl", sellerUser.SellerTable.Url);
            AddClaimIfNotNull(user.Claims, "https://openactive.io/sellerLogo", sellerUser.SellerTable.LogoUrl);
            return user;
        }

        private User GetUserFromSellerUser(SellerUserTable sellerUser)
        {
            if (sellerUser == null) return null;
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
