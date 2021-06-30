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
        public static async Task<BookingStatistics> Get(string clientId)
        {
            using (var db = await FakeBookingSystem.Database.Mem.Database.OpenAsync())
            {
                var thirtyDaysAgo = DateTimeOffset.Now.AddDays(-30);
                var bookingsByBroker = (await db.SelectAsync<OrderTable>(o => o.ClientId == clientId && o.OrderCreated > thirtyDaysAgo))
                                                .GroupBy(o => o.BrokerName)
                                                .ToDictionary(g => g.Key, g => g.Count());
                var sellersQuery = db.From<BookingPartnerTable>()
                                     .Join<BookingPartnerTable, GrantTable>((bpt, gt) => bpt.ClientId == gt.ClientId)
                                     .Join<GrantTable, SellerUserTable>((gt, st) => gt.SubjectId == st.Id.ToString())
                                     .Where<BookingPartnerTable>(bp => bp.ClientId == clientId);
                var sellersEnabled = await db.SelectAsync<SellerUserTable>(sellersQuery); // ToDo: this is only used in one of the cases - should we split this into two classes?
                return new BookingStatistics { ClientId = clientId, BookingsByBroker = bookingsByBroker, SellersEnabled = sellersEnabled.Count };
            }
        }
    }
}