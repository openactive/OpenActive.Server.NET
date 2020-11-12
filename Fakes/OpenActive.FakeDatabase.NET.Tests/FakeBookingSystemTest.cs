using Xunit;
using System.Linq;
using Xunit.Abstractions;
using OpenActive.FakeDatabase.NET;
using System;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.Data;

namespace OpenActive.FakeDatabase.NET.Test
{
    public class FakeBookingSystemTest
    {
        private readonly ITestOutputHelper output;

        public FakeBookingSystemTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void FakeDatabase_Exists()
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<ClassTable>()
                            .Join<OccurrenceTable>();

                var query = db
                    .SelectMulti<ClassTable, OccurrenceTable>(q)
                    .Select(item => new
                    {
                        title = item.Item1.Title,
                        startDate = item.Item2.Start,
                    });

                var list = query.ToList();

                foreach (var result in list)
                {
                    output.WriteLine(result.title + " " + result.startDate.ToString());
                }

                //var components = template.GetIdComponents(new Uri("https://example.com/api/session-series/asdf/events/123"));

                Assert.True(list.Count > 0);
                //Assert.Equal("session-series", components.EventType);
                //Assert.Equal("asdf", components.SessionSeriesId);
                //Assert.Equal(123, components.ScheduledSessionId);
            }         
        }

        [Fact]
        public void Transaction_Effective()
        {
            var testSeller = new SellerTable() { Name = "Test" };

            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
                {
                    db.Insert(testSeller);
                    // Note transaction.Commit(); not called
                }

                var count = db.Select<SellerTable>().Where(x => x.Id == testSeller.Id).Count();

                // As transaction did not succeed the record should not have been written
                Assert.Equal(0, count);

                // Without transaction should be able to get what was written
                var testSellerId = db.Insert(testSeller, true);
                SellerTable seller = db.SingleById<SellerTable>(testSellerId);

                Assert.Equal("Test", seller.Name);
            }   
        }

        [Fact]
        public void ReadWrite_DateTime()
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var now = DateTime.Now; // Note date must be stored as local time, not UTC
                var testOccurrence = new OccurrenceTable() { Start = now, ClassId = 1 };

                var testOccurrenceId = db.Insert(testOccurrence, true);

                OccurrenceTable occurrence = db.SingleById<OccurrenceTable>(testOccurrenceId);

                Assert.Equal(now, occurrence.Start);
            }
        }

        [Fact]
        public void ReadWrite_Enum()
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var status = BookingStatus.CustomerCancelled;
                var testOrderItem = new OrderItemsTable() { Status = status };

                var testOrderItemId = db.Insert(testOrderItem, true);

                OrderItemsTable orderItem = db.SingleById<OrderItemsTable>(testOrderItemId);

                Assert.Equal(status, orderItem.Status);
            }    
        }

        [Fact]
        public void ReadWrite_OrderWithPrice()
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var uuid = "8265ab72-d458-40aa-a460-a9619e13192c";
                decimal price = 1.3M;
                var testOrder = new OrderTable() { OrderId = uuid, SellerId = 1, TotalOrderPrice = price };

                db.Insert(testOrder, true);

                OrderTable order = db.SingleById<OrderTable>(uuid);

                Assert.Equal(price, order.TotalOrderPrice);
            }

            
        }
    }
}
