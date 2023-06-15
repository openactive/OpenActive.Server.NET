using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{
    public class BookingStatistics
    {
        public string ClientId { get; set; }
        public Dictionary<string, int> BookingsByBroker { get; set; }
        public int SellersEnabled { get; set; }

        // ToDo: this is N+1 - if we nuke the ORM, we can re-write the main query to avoid this
        public static async Task<BookingStatistics> Get(FakeBookingSystem fakeBookingSystem, string clientId)
        {
            using (var db = await fakeBookingSystem.Database.Mem.Database.OpenAsync())
            {
                var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
                var bookingsByBroker = (await db.SelectAsync<OrderTable>(o => o.ClientId == clientId && o.OrderCreated > thirtyDaysAgo))
                                                .GroupBy(o => o.BrokerName)
                                                .ToDictionary(g => g.Key, g => g.Count());
                var sellersQuery = db.From<BookingPartnerTable>()
                                     .Join<BookingPartnerTable, GrantTable>((b, g) => b.ClientId == g.ClientId)
                                     .Join<GrantTable, SellerUserTable>((g, s) => g.SubjectId == s.Id.ToString())
                                     .Where<BookingPartnerTable>(b => b.ClientId == clientId);
                var sellersEnabled = await db.SelectAsync<SellerUserTable>(sellersQuery); // ToDo: this is only used in one of the cases - should we split this into two classes?
                return new BookingStatistics { ClientId = clientId, BookingsByBroker = bookingsByBroker, SellersEnabled = sellersEnabled.Count };
            }
        }
    }
}