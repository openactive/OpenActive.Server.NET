using Xunit;
using System.Linq;
using Xunit.Abstractions;
using OpenActive.FakeDatabase.NET;
using System;

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
            var query = from classes in FakeBookingSystem.Database.Classes
                        join occurances in FakeBookingSystem.Database.Occurrences
                        on classes.Id equals occurances.ClassId
                        select new
                        {
                            title = classes.Title,
                            startDate = occurances.Start,
                        };

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

        [Fact]
        public void Transaction_Effective()
        {
            var db = FakeBookingSystem.Database.Mem.Database;
            db.Execute("SELECT * FROM SellerTable");

            var testSeller = new SellerTable() { Modified = 2, Name = "Test" };

            using (var transaction = db.GetTransaction())
            {
                db.Insert(testSeller);
                //transaction.Complete();
                transaction.Dispose();
            }

            var count = db.Query<SellerTable>().Where(x => x.Id == testSeller.Id).Count();

            // As transaction did not succeed the record should not have been written
            Assert.Equal(0, count);

            // Without transaction should be able to get what was written
            db.Insert(testSeller);
            SellerTable seller = db.SingleById<SellerTable>(testSeller.Id);

            Assert.Equal("Test", seller.Name);
        }

        [Fact]
        public void ReadWrite_DateTime()
        {
            var db = FakeBookingSystem.Database.Mem.Database;
            db.Execute("SELECT * FROM OccurrenceTable");

            var now = DateTime.Now; // Note date must be stored as local time, not UTC
            var testOccurrence = new OccurrenceTable() { Start = now };

            db.Insert(testOccurrence);

            OccurrenceTable occurrence = db.SingleById<OccurrenceTable>(testOccurrence.Id);

            Assert.Equal(now, occurrence.Start);
        }

        [Fact]
        public void ReadWrite_Enum()
        {
            var db = FakeBookingSystem.Database.Mem.Database;
            db.Execute("SELECT * FROM OrderItemsTable");

            var status = BookingStatus.CustomerCancelled;
            var testOrderItem = new OrderItemsTable() { Status = status };

            db.Insert(testOrderItem);

            OrderItemsTable orderItem = db.SingleById<OrderItemsTable>(testOrderItem.Id);

            Assert.Equal(status, orderItem.Status);
        }

        [Fact]
        public void ReadWrite_OrderWithPrice()
        {
            var db = FakeBookingSystem.Database.Mem.Database;
            db.Execute("SELECT * FROM OrderTable");

            decimal price = 1.3M;
            var testOrder = new OrderTable() { OrderId = "8265ab72-d458-40aa-a460-a9619e13192c", TotalOrderPrice = price };

            db.Insert(testOrder);

            OrderTable order = db.SingleById<OrderTable>(testOrder.Id);

            Assert.Equal(price, order.TotalOrderPrice);
        }
    }
}
