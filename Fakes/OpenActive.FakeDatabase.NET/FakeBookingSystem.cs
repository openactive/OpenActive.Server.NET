using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using Bogus;
using System.Data.SQLite;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace OpenActive.FakeDatabase.NET
{
    /// <summary>
    /// This class models the database schema within an actual booking system.
    /// It is designed to simulate the database that would be available in a full implementation.
    /// </summary>
    public static class FakeBookingSystem
    {
        /// <summary>
        /// The Database is created as static, to simulate the persistence of a real database
        /// 
        /// TODO: Move this initialisation data into an embedded string to increase portability / ease of installation
        /// </summary>
        public static FakeDatabase Database { get; } = FakeDatabase.GetPrepopulatedFakeDatabase();// JsonConvert.DeserializeObject<FakeBookingSystem>(File.ReadAllText($"../../../../fakedata.json"));

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }

    public class InMemorySQLite
    {
        public OrmLiteConnectionFactory Database;

        public InMemorySQLite()
        {
            // ServiceStack registers a memory cache client by default <see href="https://docs.servicestack.net/caching">https://docs.servicestack.net/caching</see>
            const string ConnectionString = ":memory:";
            this.Database = new OrmLiteConnectionFactory(ConnectionString, SqliteDialect.Provider);

            // Create empty tables
            DatabaseCreator.CreateTables(Database);
        }
    }

    /// <summary>
    /// Result of deleting (or attempting to delete) an Order in a FakeDatabase
    /// </summary>
    public enum FakeDatabaseDeleteOrderResult
    {
        OrderWasAlreadyDeleted,
        OrderSuccessfullyDeleted,
        OrderWasNotFound
    }

    public class FakeDatabase
    {
        public InMemorySQLite Mem = new InMemorySQLite();

        private static readonly Faker faker = new Faker("en");

        /// <summary>
        /// TODO: Call this on a schedule from both .NET Core and .NET Framework reference implementations
        /// </summary>
        public void CleanupExpiredLeases()
        {
            using (var db = Mem.Database.Open())
            {
                var occurrenceIds = new List<long>();
                var slotIds = new List<long>();

                foreach (OrderTable order in db.Select<OrderTable>(x => x.LeaseExpires < DateTimeOffset.Now))
                {
                    occurrenceIds.AddRange(db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId && x.OccurrenceId != 0).Select(x => x.OccurrenceId));
                    slotIds.AddRange(db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId && x.SlotId != 0).Select(x => x.SlotId));
                    db.Delete<OrderItemsTable>(x => x.OrderId == order.OrderId);
                    db.Delete<OrderTable>(x => x.OrderId == order.OrderId);
                }

                RecalculateSpaces(occurrenceIds.Distinct());
                RecalculateSlotUses(slotIds.Distinct());
            }
        }

        public bool AddLease(string clientId, string uuid, BrokerRole brokerRole, string brokerName, long? sellerId, string customerEmail, DateTimeOffset leaseExpires, FakeDatabaseTransaction transaction)
        {
            using (var db = Mem.Database.Open())
            {
                if (transaction != null) transaction.OrdersIds.Add(uuid);

                var existingOrder = db.Single<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid);
                if (existingOrder == null)
                {
                    db.Insert(new OrderTable
                    {
                        ClientId = clientId,
                        OrderId = uuid,
                        Deleted = false,
                        BrokerRole = brokerRole,
                        BrokerName = brokerName,
                        SellerId = sellerId ?? default,
                        CustomerEmail = customerEmail,
                        IsLease = true,
                        LeaseExpires = leaseExpires.DateTime,
                        VisibleInFeed = false
                    });
                    return true;
                }
                // Return false if there's a clash
                else if (!existingOrder.IsLease || existingOrder.Deleted)
                {
                    return false;
                }
                // Reuse existing lease if it exists
                else
                {
                    existingOrder.BrokerRole = brokerRole;
                    existingOrder.BrokerName = brokerName;
                    existingOrder.SellerId = sellerId ?? default;
                    existingOrder.CustomerEmail = customerEmail;
                    existingOrder.IsLease = true;
                    existingOrder.LeaseExpires = leaseExpires.DateTime;
                    db.Update(existingOrder);

                    return true;
                }
            }
        }

        public void DeleteLease(string clientId, string uuid, long? sellerId)
        {
            using (var db = Mem.Database.Open())
            {
                // TODO: Note this should throw an error if the Seller ID does not match, same as DeleteOrder
                if (db.Exists<OrderTable>(x => x.ClientId == clientId && x.IsLease && x.OrderId == uuid && (!sellerId.HasValue || x.SellerId == sellerId)))
                {
                    var occurrenceIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId != 0).Select(x => x.OccurrenceId).Distinct();
                    var slotIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId != 0).Select(x => x.SlotId).Distinct();

                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid);
                    db.Delete<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid);

                    RecalculateSpaces(occurrenceIds);
                    RecalculateSlotUses(slotIds);
                }
            }
        }

        public bool AddOrder(string clientId, string uuid, BrokerRole brokerRole, string brokerName, long? sellerId, string customerEmail, string paymentIdentifier, decimal totalOrderPrice, FakeDatabaseTransaction transaction)
        {
            using (var db = Mem.Database.Open())
            {
                transaction.OrdersIds.Add(uuid);

                var existingOrder = db.Single<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid);
                if (existingOrder == null)
                {
                    db.Insert(new OrderTable
                    {
                        ClientId = clientId,
                        OrderId = uuid,
                        Deleted = false,
                        BrokerRole = brokerRole,
                        BrokerName = brokerName,
                        SellerId = sellerId ?? default,
                        CustomerEmail = customerEmail,
                        PaymentIdentifier = paymentIdentifier,
                        TotalOrderPrice = totalOrderPrice,
                        IsLease = false,
                        VisibleInFeed = false
                    });
                    return true;
                }
                // Return false if there's a clash
                else if (!existingOrder.IsLease || existingOrder.Deleted)
                {
                    return false;
                }
                // Reuse existing lease if it exists
                else
                {
                    existingOrder.BrokerRole = brokerRole;
                    existingOrder.BrokerName = brokerName;
                    existingOrder.SellerId = sellerId ?? default;
                    existingOrder.CustomerEmail = customerEmail;
                    existingOrder.PaymentIdentifier = paymentIdentifier;
                    existingOrder.TotalOrderPrice = totalOrderPrice;
                    existingOrder.IsLease = false;
                    db.Update(existingOrder);

                    return true;
                }
            }
        }

        public FakeDatabaseDeleteOrderResult DeleteOrder(string clientId, string uuid, long? sellerId)
        {
            using (var db = Mem.Database.Open())
            {
                // Set the Order to deleted in the feed, and erase all associated personal data
                var order = db.Single<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid && !x.IsLease);
                if (order == null)
                {
                    return FakeDatabaseDeleteOrderResult.OrderWasNotFound;
                }
                if (order.Deleted)
                {
                    return FakeDatabaseDeleteOrderResult.OrderWasAlreadyDeleted;
                }
                if (sellerId.HasValue && order.SellerId != sellerId)
                {
                    throw new ArgumentException("SellerId does not match Order");
                }
                order.Deleted = true;
                order.CustomerEmail = null;
                order.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(order);

                var occurrenceIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId && x.OccurrenceId != 0).Select(x => x.OccurrenceId).Distinct();
                var slotIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId != 0).Select(x => x.SlotId).Distinct();
                db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId);

                RecalculateSpaces(occurrenceIds);
                RecalculateSlotUses(slotIds);

                return FakeDatabaseDeleteOrderResult.OrderSuccessfullyDeleted;
            }
        }

        public void RollbackOrder(string uuid)
        {
            using (var db = Mem.Database.Open())
            {
                // Set the Order to deleted in the feed, and erase all associated personal data
                var occurrenceIds = db.Select<OrderItemsTable>(x => x.OrderId == uuid && x.OccurrenceId != 0).Select(x => x.OccurrenceId).Distinct();
                var slotIds = db.Select<OrderItemsTable>(x => x.OrderId == uuid && x.SlotId != 0).Select(x => x.SlotId).Distinct();

                db.Delete<OrderTable>(x => x.OrderId == uuid);
                db.Delete<OrderItemsTable>(x => x.OrderId == uuid);

                RecalculateSpaces(occurrenceIds);
                RecalculateSlotUses(slotIds);
            }
        }

        public bool LeaseOrderItemsForClassOccurrence(string clientId, long? sellerId, string uuid, long occurrenceId, long numberOfSpaces)
        {
            using (var db = Mem.Database.Open())
            {
                var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                var thisClass = db.Single<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

                if (thisOccurrence != null && thisClass != null)
                {
                    if (sellerId.HasValue && thisClass.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match Order");
                    }

                    // Remove existing leases
                    // Note a real implementation would likely maintain existing leases instead of removing and recreating them
                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId == occurrenceId);
                    RecalculateSpaces(occurrenceId);

                    // Only lease if all spaces requested are available
                    if (thisOccurrence.RemainingSpaces - thisOccurrence.LeasedSpaces >= numberOfSpaces)
                    {
                        for (int i = 0; i < numberOfSpaces; i++)
                        {
                            db.Insert(new OrderItemsTable
                            {
                                ClientId = clientId,
                                Deleted = false,
                                OrderId = uuid,
                                OccurrenceId = occurrenceId
                            });
                        }

                        // Update number of spaces remaining for the opportunity
                        RecalculateSpaces(occurrenceId);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool LeaseOrderItemsForFacilitySlot(string clientId, long? sellerId, string uuid, long slotId, long numberOfSpaces)
        {
            using (var db = Mem.Database.Open())
            {
                var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
                var thisFacility = db.Single<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

                if (thisSlot != null && thisFacility != null)
                {
                    if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match Order");
                    }

                    // Remove existing leases
                    // Note a real implementation would likely maintain existing leases instead of removing and recreating them
                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId == slotId);
                    RecalculateSlotUses(slotId);

                    // Only lease if all spaces requested are available
                    if (thisSlot.RemainingUses - thisSlot.LeasedUses >= numberOfSpaces)
                    {
                        for (int i = 0; i < numberOfSpaces; i++)
                        {
                            db.Insert(new OrderItemsTable
                            {
                                ClientId = clientId,
                                Deleted = false,
                                OrderId = uuid,
                                SlotId = slotId
                            });
                        }

                        // Update number of spaces remaining for the opportunity
                        RecalculateSlotUses(slotId);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        // TODO this should reuse code of LeaseOrderItemsForClassOccurrence
        public List<long> BookOrderItemsForClassOccurrence(string clientId, long? sellerId, string uuid, long occurrenceId, string opportunityJsonLdType, string opportunityJsonLdId, string offerJsonLdId, long numberOfSpaces)
        {
            using (var db = Mem.Database.Open())
            {
                var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                var thisClass = db.Single<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

                if (thisOccurrence != null && thisClass != null)
                {
                    if (sellerId.HasValue && thisClass.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match Order");
                    }

                    // Remove existing leases
                    // Note a real implementation would likely maintain existing leases instead of removing and recreating them
                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId == occurrenceId);
                    RecalculateSpaces(occurrenceId);

                    // Only lease if all spaces requested are available
                    if (thisOccurrence.RemainingSpaces - thisOccurrence.LeasedSpaces >= numberOfSpaces)
                    {
                        var OrderItemIds = new List<long>();
                        for (int i = 0; i < numberOfSpaces; i++)
                        {
                            var orderItemId = db.Insert(new OrderItemsTable
                            {
                                ClientId = clientId,
                                Deleted = false,
                                OrderId = uuid,
                                Status = BookingStatus.Confirmed,
                                OccurrenceId = occurrenceId,
                                OpportunityJsonLdType = opportunityJsonLdType,
                                OpportunityJsonLdId = opportunityJsonLdId,
                                OfferJsonLdId = offerJsonLdId,
                                // Include the price locked into the OrderItem as the opportunity price may change
                                Price = thisClass.Price.Value
                            }, true);
                            OrderItemIds.Add(orderItemId);
                        }

                        RecalculateSpaces(occurrenceId);

                        return OrderItemIds;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }


        // TODO this should reuse code of LeaseOrderItemsForFacilityOccurrence
        public List<long> BookOrderItemsForFacilitySlot(string clientId, long? sellerId, string uuid, long slotId, string opportunityJsonLdType, string opportunityJsonLdId, string offerJsonLdId, long numberOfSpaces)
        {
            using (var db = Mem.Database.Open())
            {
                var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
                var thisFacility = db.Single<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

                if (thisSlot != null && thisFacility != null)
                {
                    if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match Order");
                    }

                    // Remove existing leases
                    // Note a real implementation would likely maintain existing leases instead of removing and recreating them
                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId == slotId);
                    RecalculateSlotUses(slotId);

                    // Only lease if all spaces requested are available
                    if (thisSlot.RemainingUses - thisSlot.LeasedUses >= numberOfSpaces)
                    {
                        var OrderItemIds = new List<long>();
                        for (int i = 0; i < numberOfSpaces; i++)
                        {
                            var orderItemId = db.Insert(new OrderItemsTable
                            {
                                ClientId = clientId,
                                Deleted = false,
                                OrderId = uuid,
                                Status = BookingStatus.Confirmed,
                                SlotId = slotId,
                                OpportunityJsonLdType = opportunityJsonLdType,
                                OpportunityJsonLdId = opportunityJsonLdId,
                                OfferJsonLdId = offerJsonLdId,
                                // Include the price locked into the OrderItem as the opportunity price may change
                                Price = thisFacility.Price.Value
                            }, true);
                            OrderItemIds.Add(orderItemId);
                        }

                        RecalculateSlotUses(slotId);

                        return OrderItemIds;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public bool CancelOrderItem(string clientId, long? sellerId, string uuid, List<long> orderItemIds, bool customerCancelled)
        {
            using (var db = Mem.Database.Open())
            {
                var order = db.Single<OrderTable>(x => x.ClientId == clientId && !x.IsLease && x.OrderId == uuid && !x.Deleted);
                if (order != null)
                {
                    if (sellerId.HasValue && order.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match Order");
                    }
                    List<OrderItemsTable> updatedOrderItems = new List<OrderItemsTable>();
                    foreach (OrderItemsTable orderItem in db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId && orderItemIds.Contains(x.Id)))
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.Attended)
                        {
                            updatedOrderItems.Add(orderItem);
                            db.UpdateOnly(() => new OrderItemsTable { Status = customerCancelled ? BookingStatus.CustomerCancelled : BookingStatus.SellerCancelled });
                        }
                    }
                    // Update the total price and modified date on the Order to update the feed, if something has changed
                    if (updatedOrderItems.Count > 0)
                    {
                        var totalPrice = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Sum(x => x.Price);
                        order.TotalOrderPrice = totalPrice;
                        order.VisibleInFeed = true;
                        order.Modified = DateTimeOffset.Now.UtcTicks;
                        db.Update(order);
                        // Note an actual implementation would need to handle different opportunity types here
                        // Update the number of spaces available as a result of cancellation
                        RecalculateSpaces(updatedOrderItems.Where(x => x.OccurrenceId != 0).Select(x => x.OccurrenceId).Distinct());
                        RecalculateSlotUses(updatedOrderItems.Where(x => x.SlotId != 0).Select(x => x.SlotId).Distinct());
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void RecalculateSlotUses(long slotId)
        {
            RecalculateSlotUses(new List<long> { slotId });
        }

        public void RecalculateSlotUses(IEnumerable<long> slotIds)
        {
            using (var db = Mem.Database.Open())
            {
                foreach (var slotId in slotIds)
                {
                    var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);

                    // Update number of leased spaces remaining for the opportunity
                    var leasedUses = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.IsLease && x.OccurrenceId == slotId).Count();
                    thisSlot.LeasedUses = leasedUses;

                    // Update number of actual spaces remaining for the opportunity
                    var totalUsesTaken = db.LoadSelect<OrderItemsTable>(x => !x.OrderTable.IsLease && x.OccurrenceId == slotId && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Count();
                    thisSlot.RemainingUses = thisSlot.MaximumUses - totalUsesTaken;

                    // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition) 
                    // TODO: Document this!
                    thisSlot.Modified = DateTimeOffset.Now.UtcTicks;
                    db.Update(thisSlot);
                }
            }
        }

        public void RecalculateSpaces(long occurrenceId)
        {
            RecalculateSpaces(new List<long> { occurrenceId });
        }

        public void RecalculateSpaces(IEnumerable<long> occurrenceIds)
        {
            using (var db = Mem.Database.Open())
            {
                foreach (var occurrenceId in occurrenceIds)
                {
                    var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);

                    // Update number of leased spaces remaining for the opportunity
                    var leasedSpaces = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.IsLease && x.OccurrenceId == occurrenceId).Count();
                    thisOccurrence.LeasedSpaces = leasedSpaces;

                    // Update number of actual spaces remaining for the opportunity
                    var totalSpacesTaken = db.LoadSelect<OrderItemsTable>(x => !x.OrderTable.IsLease && x.OccurrenceId == occurrenceId && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Count();
                    thisOccurrence.RemainingSpaces = thisOccurrence.TotalSpaces - totalSpacesTaken;

                    // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition) // TODO: Document this!
                    thisOccurrence.Modified = DateTimeOffset.Now.UtcTicks;
                    db.Update(thisOccurrence);
                }
            }
        }

        public static FakeDatabase GetPrepopulatedFakeDatabase()
        {
            var db = new FakeDatabase();
            db.CreateFakeClasses();
            db.CreateFakeFacilitiesAndSlots();
            return db;
        }

        private void CreateFakeFacilitiesAndSlots()
        {
            using (var db = Mem.Database.Open())
            {
                var slots = Enumerable.Range(1, 1000)
                .Select(n => new {
                    Id = n,
                    StartDate = faker.Date.Soon(10).Truncate(TimeSpan.FromSeconds(1)),
                    TotalUses = faker.Random.Int(0, 50)
                })
                .Select(x => new SlotTable
                {
                    FacilityUseId = Decimal.ToInt32(x.Id / 10),
                    Id = x.Id,
                    Deleted = false,
                    Start = x.StartDate,
                    End = x.StartDate + TimeSpan.FromMinutes(faker.Random.Int(30, 360)),
                    MaximumUses = x.TotalUses,
                    RemainingUses = x.TotalUses
                })
                .ToList();

                var facilities = Enumerable.Range(1, 100)
                .Select(id => new FacilityUseTable
                {
                    Id = id,
                    Deleted = false,
                    Name = faker.Commerce.ProductMaterial() + " " + faker.PickRandomParam("Sports Hall", "Swimming Pool Hall", "Running Hall", "Jumping Hall"),
                    Price = Decimal.Parse(faker.Random.Bool() ? "0.00" : faker.Commerce.Price(0, 20)),
                    SellerId = faker.Random.Long(1, 2)
                })
                .ToList();

                db.InsertAll(slots);
                db.InsertAll(facilities);
            }
        }

        public void CreateFakeClasses()
        {
            var occurrences = Enumerable.Range(1, 1000)
            .Select(n => new {
                Id = n,
                StartDate = faker.Date.Soon(10).Truncate(TimeSpan.FromSeconds(1)),
                TotalSpaces = faker.Random.Int(0, 50)
            })
            .Select(x => new OccurrenceTable
            {
                ClassId = Decimal.ToInt32(x.Id / 10),
                Id = x.Id,
                Deleted = false,
                Start = x.StartDate,
                End = x.StartDate + TimeSpan.FromMinutes(faker.Random.Int(30, 360)),
                TotalSpaces = x.TotalSpaces,
                RemainingSpaces = x.TotalSpaces
            })
            .ToList();

            var classes = Enumerable.Range(1, 100)
            .Select(id => new ClassTable
            {
                Id = id,
                Deleted = false,
                Title = faker.Commerce.ProductMaterial() + " " + faker.PickRandomParam("Yoga", "Zumba", "Walking", "Cycling", "Running", "Jumping"),
                Price = Decimal.Parse(faker.Random.Bool() ? "0.00" : faker.Commerce.Price(0, 20)),
                SellerId = faker.Random.Long(1, 2)
            })
            .ToList();

            var sellers = new List<SellerTable> {
                new SellerTable { Id = 1, Name = "Acme Fitness Ltd", IsIndividual = false },
                new SellerTable { Id = 2, Name = "Jane Smith", IsIndividual = true }
            };

            using (var db = Mem.Database.Open())
            {
                db.InsertAll(occurrences);
                db.InsertAll(classes);
                db.InsertAll(sellers);
            }
        }

        public (int, int) AddClass(string testDatasetIdentifier, long seller, string title, decimal? price, DateTimeOffset startTime, DateTimeOffset endTime, long totalSpaces)
        {
            using (var db = Mem.Database.Open())
            {
                var classId = db.Insert(new ClassTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    Title = title,
                    Price = price,
                    SellerId = seller
                }, true);

                var occurrenceId = db.Insert(new OccurrenceTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    ClassId = classId,
                    Start = startTime.DateTime,
                    End = endTime.DateTime,
                    TotalSpaces = totalSpaces,
                    RemainingSpaces = totalSpaces
                }, true);

                return ((int)classId, (int)occurrenceId);
            }
        }

        public (int, int) AddFacility(string testDatasetIdentifier, long seller, string title, decimal? price, DateTimeOffset startTime, DateTimeOffset endTime, long totalUses)
        {
            using (var db = Mem.Database.Open())
            {
                var facilityId = db.Insert(new FacilityUseTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    Name = title,
                    Price = price,
                    SellerId = seller
                }, true);

                var slotId = db.Insert(new SlotTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    FacilityUseId = facilityId,
                    Start = startTime.DateTime,
                    End = endTime.DateTime,
                    MaximumUses = totalUses,
                    RemainingUses = totalUses
                }, true);

                return ((int)facilityId, (int)slotId);
            }
        }

        public void DeleteTestClassesFromDataset(string testDatasetIdentifier)
        {
            using (var db = Mem.Database.Open())
            {
                db.UpdateOnly(() => new ClassTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);

                db.UpdateOnly(() => new OccurrenceTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);
            }
        }

        public void DeleteTestFacilitiesFromDataset(string testDatasetIdentifier)
        {
            using (var db = Mem.Database.Open())
            {
                db.UpdateOnly(() => new FacilityUseTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);

                db.UpdateOnly(() => new SlotTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);
            }
        }
    }
}
