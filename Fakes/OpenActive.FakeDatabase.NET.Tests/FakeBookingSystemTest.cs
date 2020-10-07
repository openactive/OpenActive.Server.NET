using Xunit;
using System.Linq;
using Xunit.Abstractions;
using OpenActive.FakeDatabase.NET;
using System;
using ServiceStack.OrmLite;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

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
            var testSeller = new SellerTable() { Modified = 2, Name = "Test" };

            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
                {
                    db.Insert(testSeller);
                    //transaction.Complete();
                    transaction.Dispose();
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
                decimal price = 1.3M;
                var testOrder = new OrderTable() { OrderId = "8265ab72-d458-40aa-a460-a9619e13192c", SellerId = 1, TotalOrderPrice = price };

                var testOrderId = db.Insert(testOrder, true);

                OrderTable order = db.SingleById<OrderTable>(testOrderId);

                Assert.Equal(price, order.TotalOrderPrice);
            }
        }

        [Fact]
        public async Task AddClass_OneThreadNoRaceCondition()
        {
            List<(int, int)> result = await Task.Run(async () => {
                var list = new List<(int, int)>();

                for (int z = 0; z < 100; z++)
                {
                    list.Add(await FakeBookingSystem.Database.AddClass("test1", 1, "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event", 0M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 10, false));
                }

                return list;
            });

            var duplicates = CountDuplicates(result);

            Assert.Equal(0, duplicates.Item1);
            Assert.Equal(0, duplicates.Item2);
        }

        [Fact]
        public async void AddClass_TwoThreadsWithRaceCondition()
        {
            Task<List<(int, int)>> result1 = Task.Run(async () => {
                var list = new List<(int, int)>();

                for (int z = 0; z < 100; z++)
                {
                    list.Add(await FakeBookingSystem.Database.AddClass("test1", 1, "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event", 0M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 10, false));
                }

                return list;
            });

            Task<List<(int, int)>> result2 = Task.Run(async () => {
                var list = new List<(int, int)>();

                for (int z = 0; z < 100; z++)
                {
                    list.Add(await FakeBookingSystem.Database.AddClass("test1", 1, "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event", 0M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 10, false));
                }

                return list;
            });

            var result = (await result1).Concat(await result2);

            Console.WriteLine(CountDuplicates(result));

            var duplicates = CountDuplicates(result);

            Assert.Equal(0, duplicates.Item1);
            Assert.Equal(0, duplicates.Item2);
        }

        private (int, int) CountDuplicates(IEnumerable<(int, int)> list)
        {
            var set1 = new HashSet<int>();
            var set2 = new HashSet<int>();

            var cnt1 = 0;
            var cnt2 = 0;

            foreach (var tuple in list)
            {
                if (set1.Contains(tuple.Item1))
                {
                    cnt1++;
                }
                else
                {
                    set1.Add(tuple.Item1);
                }

                if (set2.Contains(tuple.Item2))
                {
                    cnt2++;
                }
                else
                {
                    set2.Add(tuple.Item2);
                }
            }

            return (cnt1, cnt2);
        }
    }
}
