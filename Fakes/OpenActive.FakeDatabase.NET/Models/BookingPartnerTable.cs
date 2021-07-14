using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{
    public class BookingPartnerTable
    {
        [PrimaryKey]
        public string ClientId { get; set; }
        public string Name { get; set; }
        public string ClientSecret { get; set; }
        public string ClientUri { get; set; }
        public string RestoreAccessUri { get; set; }
        public string LogoUri { get; set; }
        public string[] GrantTypes { get; set; }
        public string[] RedirectUris { get; set; }
        public string Scope { get; set; }
        public bool Registered { get; set; }
        public DateTime CreatedDate { get; set; }
        public string InitialAccessToken { get; set; }
        public DateTime InitialAccessTokenKeyValidUntil { get; set; }
        public bool BookingsSuspended { get; set; }
        public string Email { get; set; }

        public static async Task Create(IDbConnection db)
        {
            var bookingPartners = new List<BookingPartnerTable>
            {
                new BookingPartnerTable { Name = "RED January", ClientId = Guid.NewGuid().ToString(), InitialAccessToken = "openactive_test_suite_client_12345xaq", Registered = false, InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now },
                new BookingPartnerTable { Name = "Acmefitness", ClientId = "clientid_800", ClientSecret = "secret".Sha256(), Email="garden@health.com", Registered = true, InitialAccessToken = "98767", InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now, BookingsSuspended = false,
                    Scope = "openid profile openactive-openbooking openactive-ordersfeed openactive-identity",
                    GrantTypes = new[] { "client_credentials", "refresh_token", "authorization_code" },
                    ClientUri = "http://example.com",
                    RestoreAccessUri = "http://example.com",
                    LogoUri = "https://via.placeholder.com/512x256.png?text=Logo",
                    RedirectUris = new[] { "http://localhost:3000/cb" }
                },
                new BookingPartnerTable { Name = "Mum's runs", ClientId = "clientid_801", ClientSecret = "secret".Sha256(), Email="garden@health.com", Registered = true, InitialAccessToken = "98768", InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now, BookingsSuspended = false,
                    Scope = "openid profile openactive-openbooking openactive-ordersfeed openactive-identity",
                    GrantTypes = new[] { "client_credentials", "refresh_token", "authorization_code" },
                    ClientUri = "http://example.com",
                    RestoreAccessUri = "http://example.com",
                    LogoUri = "https://via.placeholder.com/512x256.png?text=Logo",
                    RedirectUris = new[] { "http://localhost:3000/cb" }
                },
                new BookingPartnerTable { Name = "Vivacity Insurance", ClientId = Guid.NewGuid().ToString(), InitialAccessToken = "dynamic-primary-745ddf2d13019ce8b69c", Registered = false, InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now },
                new BookingPartnerTable { Name = "Active with Friends", ClientId = Guid.NewGuid().ToString(), InitialAccessToken = "dynamic-secondary-a21518cb57af7b6052df", Registered = false, InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now },
                new BookingPartnerTable { Name = "Ultimate Fitness Challenge", ClientId = "clientid_XXX", ClientSecret = "secret".Sha256(), Email="garden@health.com", Registered = true, InitialAccessToken = "123123", InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now, BookingsSuspended = false,
                    Scope = "openid profile openactive-openbooking openactive-ordersfeed openactive-identity",
                    GrantTypes = new[] { "client_credentials", "refresh_token", "authorization_code" },
                    ClientUri = "http://example.com",
                    RestoreAccessUri = "http://example.com",
                    LogoUri = "https://via.placeholder.com/512x256.png?text=Logo",
                    RedirectUris = new[] { "http://localhost:3000/cb" }
                },
                new BookingPartnerTable { Name = "Healthy Steps Every Day", ClientId = "clientid_YYY", ClientSecret = "secret".Sha256(), Email="garden@health.com", Registered = true, InitialAccessToken = "456456", InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(1), CreatedDate = DateTime.Now, BookingsSuspended = false,
                    Scope = "openid profile openactive-openbooking openactive-ordersfeed openactive-identity",
                    GrantTypes = new[] { "client_credentials", "refresh_token", "authorization_code" },
                    ClientUri = "http://example.com",
                    RestoreAccessUri = "http://example.com",
                    LogoUri = "https://via.placeholder.com/512x256.png?text=Logo",
                    RedirectUris = new[] { "http://localhost:3000/cb" }
                },
            };

            await db.InsertAllAsync(bookingPartners);
            // To populate GrantTable locally, run the tests, e.g. `NODE_APP_INSTANCE=dev npm run start auth non-free`
        }

        public static async Task<List<BookingPartnerTable>> Get()
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                return await db.SelectAsync<BookingPartnerTable>();
            }
        }

        public static async Task<List<BookingPartnerTable>> GetBySellerUserId(long sellerUserId)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                var query = db.From<BookingPartnerTable>()
                              .Join<BookingPartnerTable, GrantTable>((b, g) => b.ClientId == g.ClientId && g.Type == "user_consent")
                              .Join<GrantTable, SellerUserTable>((g, s) => g.SubjectId == s.Id.ToString())
                              .Where<SellerUserTable>(s => s.Id == sellerUserId);
                return await db.SelectAsync(query);
            }
        }

        public static async Task<BookingPartnerTable> GetByInitialAccessToken(string registrationKey)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.InitialAccessToken == registrationKey);
                return bookingPartner?.InitialAccessTokenKeyValidUntil > DateTime.Now ? bookingPartner : null;
            }
        }

        public static async Task<BookingPartnerTable> GetByClientId(string clientId)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                return await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
            }
        }

        public static async Task Save(BookingPartnerTable bookingPartnerTable)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                await db.SaveAsync(bookingPartnerTable);
            }
        }

        public static async Task ResetKey(string clientId, string key)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.Registered = false;
                bookingPartner.InitialAccessToken = key;
                bookingPartner.InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(2);
                bookingPartner.ClientSecret = null;
                await db.SaveAsync(bookingPartner);
            }
        }

        public static async Task SetKey(string clientId, string key)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.InitialAccessToken = key;
                bookingPartner.InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(2);
                await db.SaveAsync(bookingPartner);
            }
        }

        public static async Task UpdateScope(string clientId, string scope, bool bookingsSuspended)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.Scope = scope;
                bookingPartner.BookingsSuspended = true;
                await db.SaveAsync(bookingPartner);
            }
        }

        public static async Task Add(BookingPartnerTable newBookingPartner)
        {
            using (var db = await FakeBookingSystem.FakeDatabase.DatabaseWrapper.Database.OpenAsync())
            {
                await db.SaveAsync(newBookingPartner);
            }
        }
    }
}