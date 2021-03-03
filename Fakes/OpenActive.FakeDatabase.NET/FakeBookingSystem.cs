﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Bogus;
using OpenActive.FakeDatabase.NET.Helpers;
using ServiceStack.OrmLite;

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
        /// </summary>
        public static FakeDatabase Database { get; } = FakeDatabase.GetPrepopulatedFakeDatabase();

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }

    // ReSharper disable once InconsistentNaming
    public class InMemorySQLite
    {
        public readonly OrmLiteConnectionFactory Database;

        public InMemorySQLite()
        {
            // ServiceStack registers a memory cache client by default <see href="https://docs.servicestack.net/caching">https://docs.servicestack.net/caching</see>
            // There are issues with transactions when using full in-memory SQLite. To workaround this, we create a temporary file and use this to hold the SQLite database.
            string connectionString = Path.GetTempPath() + "fakedatabase.db";
            Database = new OrmLiteConnectionFactory(connectionString, SqliteDialect.Provider);

            using (var connection = Database.Open())
            {
                // Enable write-ahead logging
                var walCommand = connection.CreateCommand();
                walCommand.CommandText =
                @"
    PRAGMA journal_mode = 'wal'
";
                walCommand.ExecuteNonQuery();
            }

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

    /// <summary>
    /// Result of booking (or attempting to book) an OrderProposal in a FakeDatabase
    /// </summary>
    public enum FakeDatabaseBookOrderProposalResult
    {
        OrderProposalVersionOutdated,
        OrderSuccessfullyBooked,
        OrderWasNotFound,
        OrderProposalNotAccepted
    }

    /// <summary>
    /// Result of booking (or attempting to book) an OrderProposal in a FakeDatabase
    /// </summary>
    public enum ReserveOrderItemsResult
    {
        Success,
        SellerIdMismatch,
        OpportunityNotFound,
        OpportunityOfferPairNotBookable,
        NotEnoughCapacity
    }


    public class FakeDatabase
    {
        private const float ProportionWithRequiresAttendeeValidation = 1f / 10;

        public readonly InMemorySQLite Mem = new InMemorySQLite();

        private static readonly Faker Faker = new Faker();

        private const int OpportunityCount = 1000;

        /// <summary>
        /// TODO: Call this on a schedule from both .NET Core and .NET Framework reference implementations
        /// </summary>
        public void CleanupExpiredLeases()
        {
            using (var db = Mem.Database.Open())
            {
                var occurrenceIds = new List<long>();
                var slotIds = new List<long>();

                foreach (var order in db.Select<OrderTable>(x => x.LeaseExpires < DateTimeOffset.Now))
                {
                    // ReSharper disable twice PossibleInvalidOperationException
                    occurrenceIds.AddRange(db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId && x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value));
                    slotIds.AddRange(db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId && x.SlotId.HasValue).Select(x => x.SlotId.Value));
                    db.Delete<OrderItemsTable>(x => x.OrderId == order.OrderId);
                    db.Delete<OrderTable>(x => x.OrderId == order.OrderId);
                }

                RecalculateSpaces(db, occurrenceIds.Distinct());
                RecalculateSlotUses(db, slotIds.Distinct());
            }
        }

        public static bool AddLease(string clientId, string uuid, BrokerRole brokerRole, string brokerName, long? sellerId, string customerEmail, DateTimeOffset leaseExpires, FakeDatabaseTransaction transaction)
        {
            var db = transaction.DatabaseConnection;

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
                    SellerId = sellerId ?? 1,
                    CustomerEmail = customerEmail,
                    OrderMode = OrderMode.Lease,
                    LeaseExpires = leaseExpires.DateTime,
                    VisibleInFeed = false
                });
                return true;
            }
            // Return false if there's a clash with an existing Order or OrderProposal
            else if (existingOrder.OrderMode != OrderMode.Lease || existingOrder.Deleted)
            {
                return false;
            }
            // Reuse existing lease if it exists
            else
            {
                existingOrder.BrokerRole = brokerRole;
                existingOrder.BrokerName = brokerName;
                existingOrder.SellerId = sellerId ?? 1;
                existingOrder.CustomerEmail = customerEmail;
                existingOrder.OrderMode = OrderMode.Lease;
                existingOrder.LeaseExpires = leaseExpires.DateTime;
                db.Update(existingOrder);

                return true;
            }
        }

        /// <summary>
        /// Update logistics data for FacilityUse to trigger logistics change notification
        /// </summary>
        /// <param name="slotId"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool UpdateFacilityUseName(long slotId, string newName)
        {
            using (var db = Mem.Database.Open())
            {
                var query = db.From<SlotTable>()
                            .LeftJoin<SlotTable, FacilityUseTable>()
                            .Where(x => x.Id == slotId)
                            .And<FacilityUseTable>(y => !y.Deleted);
                var facilityUse = db.Select<FacilityUseTable>(query).Single();
                if (facilityUse == null)
                {
                    return false;
                }

                facilityUse.Name = newName;
                facilityUse.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(facilityUse);
                return true;
            }
        }

        /// <summary>
        /// Update logistics data for Slot to trigger logistics change notification
        /// </summary>
        /// <param name="slotId"></param>
        /// <param name="numberOfMins"></param>
        /// <returns></returns>
        public bool UpdateFacilitySlotStartAndEndTimeByPeriodInMins(long slotId, int numberOfMins)
        {
            using (var db = Mem.Database.Open())
            {
                var slot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
                if (slot == null)
                {
                    return false;
                }

                slot.Start.AddMinutes(numberOfMins);
                slot.End.AddMinutes(numberOfMins);
                slot.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(slot);
                return true;
            }
        }

        /// <summary>
        /// Update location based logistics data for FacilityUse to trigger logistics change notification
        /// </summary>
        /// <param name="slotId"></param>
        /// <param name="newLat"></param>
        /// <param name="newLng"></param>
        /// <returns></returns>
        public bool UpdateFacilityUseLocationLatLng(long slotId, decimal newLat, decimal newLng)
        {
            using (var db = Mem.Database.Open())
            {
                var query = db.From<SlotTable>()
                            .LeftJoin<SlotTable, FacilityUseTable>()
                            .Where(x => x.Id == slotId)
                            .And<FacilityUseTable>(y => !y.Deleted);
                var facilityUse = db.Select<FacilityUseTable>(query).Single();
                if (facilityUse == null)
                {
                    return false;
                }

                facilityUse.LocationLat = newLat;
                facilityUse.LocationLng = newLng;
                facilityUse.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(facilityUse);
                return true;
            }
        }

        /// <summary>
        /// Update name logistics data for SessionSeries to trigger logistics change notification
        /// </summary>
        /// <param name="occurrenceId"></param>
        /// <param name="newTitle"></param>
        /// <returns></returns>
        public bool UpdateClassTitle(long occurrenceId, string newTitle)
        {
            using (var db = Mem.Database.Open())
            {
                var query = db.From<OccurrenceTable>()
                            .LeftJoin<OccurrenceTable, ClassTable>()
                            .Where(x => x.Id == occurrenceId)
                            .And<ClassTable>(y => !y.Deleted);
                var classInstance = db.Select<ClassTable>(query).Single();
                if (classInstance == null)
                {
                    return false;
                }

                classInstance.Title = newTitle;
                classInstance.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(classInstance);
                return true;
            }
        }

        /// <summary>
        /// Update time based logistics data for ScheduledSession to trigger logistics change notification
        /// </summary>
        /// <param name="occurrenceId"></param>
        /// <param name="numberOfMins"></param>
        /// <returns></returns>
        public bool UpdateScheduledSessionStartAndEndTimeByPeriodInMins(long occurrenceId, int numberOfMins)
        {
            using (var db = Mem.Database.Open())
            {
                var occurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                if (occurrence == null)
                {
                    return false;
                }

                occurrence.Start.AddMinutes(numberOfMins);
                occurrence.End.AddMinutes(numberOfMins);
                occurrence.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(occurrence);
                return true;
            }
        }

        /// <summary>
        /// Update location based logistics data for SessionSeries to trigger logistics change notification
        /// </summary>
        /// <param name="occurrenceId"></param>
        /// <param name="newLat"></param>
        /// <param name="newLng"></param>
        /// <returns></returns>
        public bool UpdateSessionSeriesLocationLatLng(long occurrenceId, decimal newLat, decimal newLng)
        {
            using (var db = Mem.Database.Open())
            {
                var query = db.From<OccurrenceTable>()
                            .LeftJoin<OccurrenceTable, ClassTable>()
                            .Where(x => x.Id == occurrenceId)
                            .And<ClassTable>(y => !y.Deleted);
                var classInstance = db.Select<ClassTable>(query).Single();
                if (classInstance == null)
                {
                    return false;
                }

                classInstance.LocationLat = newLat;
                classInstance.LocationLng = newLng;
                classInstance.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(classInstance);
                return true;
            }
        }

        public bool UpdateAccess(string uuid, bool updateAccessPass = false, bool updateAccessCode = false)
        {
            if (!updateAccessPass && !updateAccessCode)
            {
                return false;
            }

            using (var db = Mem.Database.Open())
            {
                OrderTable order = db.Single<OrderTable>(x => x.OrderId == uuid && !x.Deleted);

                if (order != null)
                {
                    List<OrderItemsTable> orderItems = db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId);

                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.Proposed || orderItem.Status == BookingStatus.None)
                        {
                            if (updateAccessCode)
                            {
                                orderItem.PinCode = Faker.Random.String(length: 6, minChar: '0', maxChar: '9');
                            }

                            if (updateAccessPass)
                            {
                                orderItem.ImageUrl = Faker.Image.PlaceholderUrl(width: 25, height: 25);
                                orderItem.BarCodeText = Faker.Random.String(length: 10, minChar: '0', maxChar: '9');
                            }

                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            db.Save(orderItem);
                        }
                    }

                    order.Modified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInFeed = true;
                    db.Update(order);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool UpdateOpportunityAttendance(string uuid)
        {
            using (var db = Mem.Database.Open())
            {
                OrderTable order = db.Single<OrderTable>(x => x.OrderId == uuid && !x.Deleted);

                if (order != null)
                {
                    List<OrderItemsTable> orderItems = db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId);

                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.Proposed || orderItem.Status == BookingStatus.None)
                        {
                            orderItem.Status = BookingStatus.Attended;
                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            db.Update(orderItem);
                        }
                    }

                    order.Modified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInFeed = true;
                    db.Update(order);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddCustomerNotice(string uuid)
        {
            using (var db = Mem.Database.Open())
            {
                OrderTable order = db.Single<OrderTable>(x => x.OrderId == uuid && !x.Deleted);
                if (order != null)
                {
                    List<OrderItemsTable> orderItems = db.Select<OrderItemsTable>(x => x.OrderId == order.OrderId);
                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.Proposed || orderItem.Status == BookingStatus.None)
                        {
                            orderItem.CustomerNotice = $"customer notice message: {Faker.Random.String(10, minChar: 'a', maxChar: 'z')}";
                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            db.Update(orderItem);
                        }
                    }

                    order.Modified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInFeed = true;
                    db.Update(order);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void DeleteLease(string clientId, string uuid, long? sellerId)
        {
            using (var db = Mem.Database.Open())
            {
                // TODO: Note this should throw an error if the Seller ID does not match, same as DeleteOrder
                if (db.Exists<OrderTable>(x => x.ClientId == clientId && x.OrderMode == OrderMode.Lease && x.OrderId == uuid && (!sellerId.HasValue || x.SellerId == sellerId)))
                {
                    // ReSharper disable twice PossibleInvalidOperationException
                    var occurrenceIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct();
                    var slotIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct();

                    db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid);
                    db.Delete<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid);

                    RecalculateSpaces(db, occurrenceIds);
                    RecalculateSlotUses(db, slotIds);
                }
            }
        }

        public static bool AddOrder(string clientId, string uuid, BrokerRole brokerRole, string brokerName, long? sellerId, string customerEmail, string paymentIdentifier, decimal totalOrderPrice, FakeDatabaseTransaction transaction, string proposalVersionUuid, ProposalStatus? proposalStatus)
        {
            var db = transaction.DatabaseConnection;

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
                    SellerId = sellerId ?? 1,
                    CustomerEmail = customerEmail,
                    PaymentIdentifier = paymentIdentifier,
                    TotalOrderPrice = totalOrderPrice,
                    OrderMode = proposalVersionUuid != null ? OrderMode.Proposal : OrderMode.Booking,
                    VisibleInFeed = false,
                    ProposalVersionId = proposalVersionUuid,
                    ProposalStatus = proposalStatus
                });
                return true;
            }
            // Return false if there's a clash with an existing Order or OrderProposal
            else if (existingOrder.OrderMode != OrderMode.Lease || existingOrder.Deleted)
            {
                return false;
            }
            // Reuse existing lease if it exists
            else
            {
                existingOrder.BrokerRole = brokerRole;
                existingOrder.BrokerName = brokerName;
                existingOrder.SellerId = sellerId ?? 1;
                existingOrder.CustomerEmail = customerEmail;
                existingOrder.PaymentIdentifier = paymentIdentifier;
                existingOrder.TotalOrderPrice = totalOrderPrice;
                existingOrder.OrderMode = proposalVersionUuid != null ? OrderMode.Proposal : OrderMode.Booking;
                existingOrder.ProposalVersionId = proposalVersionUuid;
                existingOrder.ProposalStatus = proposalStatus;
                db.Update(existingOrder);

                return true;
            }
        }

        public FakeDatabaseDeleteOrderResult DeleteOrder(string clientId, string uuid, long? sellerId)
        {
            using (var db = Mem.Database.Open())
            {
                // Set the Order to deleted in the feed, and erase all associated personal data
                var order = db.Single<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OrderMode != OrderMode.Lease);
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

                var occurrenceIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId && x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct();
                var slotIds = db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct();
                db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId);

                RecalculateSpaces(db, occurrenceIds);
                RecalculateSlotUses(db, slotIds);

                return FakeDatabaseDeleteOrderResult.OrderSuccessfullyDeleted;
            }
        }

        public static (ReserveOrderItemsResult, long?, long?) LeaseOrderItemsForClassOccurrence(FakeDatabaseTransaction transaction, string clientId, long? sellerId, string uuid, long occurrenceId, long spacesRequested)
        {
            var db = transaction.DatabaseConnection;
            var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
            var thisClass = db.Single<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

            if (thisOccurrence == null || thisClass == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null, null);

            if (sellerId.HasValue && thisClass.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null, null);

            if (thisClass.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisOccurrence.Start - thisClass.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId == occurrenceId);
            RecalculateSpaces(db, thisOccurrence);

            // Only lease if all spaces requested are available
            if (thisOccurrence.RemainingSpaces - thisOccurrence.LeasedSpaces < spacesRequested)
            {
                var notionalRemainingSpaces = thisOccurrence.RemainingSpaces - thisOccurrence.LeasedSpaces;
                var totalCapacityErrors = Math.Max(0, spacesRequested - notionalRemainingSpaces);
                var capacityErrorsCausedByLeasing = Math.Min(totalCapacityErrors, thisOccurrence.LeasedSpaces);
                return (ReserveOrderItemsResult.NotEnoughCapacity, totalCapacityErrors - capacityErrorsCausedByLeasing, capacityErrorsCausedByLeasing);
            }

            for (var i = 0; i < spacesRequested; i++)
            {
                db.Insert(new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid,
                    OccurrenceId = occurrenceId,
                    Status = BookingStatus.None
                });
            }

            // Update number of spaces remaining for the opportunity
            RecalculateSpaces(db, thisOccurrence);
            return (ReserveOrderItemsResult.Success, null, null);
        }

        public static (ReserveOrderItemsResult, long?, long?) LeaseOrderItemsForFacilitySlot(FakeDatabaseTransaction transaction, string clientId, long? sellerId, string uuid, long slotId, long spacesRequested)
        {
            var db = transaction.DatabaseConnection;
            var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
            var thisFacility = db.Single<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

            if (thisSlot == null || thisFacility == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null, null);

            if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null, null);

            if (thisSlot.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisSlot.Start - thisSlot.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId == slotId);
            RecalculateSlotUses(db, thisSlot);

            // Only lease if all spaces requested are available
            if (thisSlot.RemainingUses - thisSlot.LeasedUses < spacesRequested)
            {
                var notionalRemainingSpaces = thisSlot.RemainingUses - thisSlot.LeasedUses;
                var totalCapacityErrors = Math.Max(0, spacesRequested - notionalRemainingSpaces);
                var capacityErrorsCausedByLeasing = Math.Min(totalCapacityErrors, thisSlot.LeasedUses);
                return (ReserveOrderItemsResult.NotEnoughCapacity, totalCapacityErrors - capacityErrorsCausedByLeasing, capacityErrorsCausedByLeasing);
            }

            for (var i = 0; i < spacesRequested; i++)
            {
                db.Insert(new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid,
                    SlotId = slotId,
                    Status = BookingStatus.None
                });
            }

            // Update number of spaces remaining for the opportunity
            RecalculateSlotUses(db, thisSlot);
            return (ReserveOrderItemsResult.Success, null, null);
        }

        public struct BookedOrderItemInfo
        {
            public long OrderItemId { get; set; }
            public string PinCode { get; set; }
            public string ImageUrl { get; set; }
            public string BarCodeText { get; set; }
        }

        // TODO this should reuse code of LeaseOrderItemsForClassOccurrence
        public static (ReserveOrderItemsResult, List<BookedOrderItemInfo>) BookOrderItemsForClassOccurrence(
            FakeDatabaseTransaction transaction,
            string clientId,
            long? sellerId,
            string uuid,
            long occurrenceId,
            string opportunityJsonLdType,
            string opportunityJsonLdId,
            string offerJsonLdId,
            long numberOfSpaces,
            bool proposal
            )
        {
            var db = transaction.DatabaseConnection;
            var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
            var thisClass = db.Single<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

            if (thisOccurrence == null || thisClass == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null);

            if (sellerId.HasValue && thisClass.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null);

            if (thisClass.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisOccurrence.Start - thisClass.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.OccurrenceId == occurrenceId);
            RecalculateSpaces(db, thisOccurrence);

            // Only lease if all spaces requested are available
            if (thisOccurrence.RemainingSpaces - thisOccurrence.LeasedSpaces < numberOfSpaces)
                return (ReserveOrderItemsResult.NotEnoughCapacity, null);

            var bookedOrderItemInfos = new List<BookedOrderItemInfo>();
            for (var i = 0; i < numberOfSpaces; i++)
            {
                var orderItem = new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid,
                    Status = proposal ? BookingStatus.Proposed : BookingStatus.Confirmed,
                    OccurrenceId = occurrenceId,
                    OpportunityJsonLdType = opportunityJsonLdType,
                    OpportunityJsonLdId = opportunityJsonLdId,
                    OfferJsonLdId = offerJsonLdId,
                    // Include the price locked into the OrderItem as the opportunity price may change
                    Price = thisClass.Price.Value,
                    PinCode = Faker.Random.String(length: 6, minChar: '0', maxChar: '9'),
                    ImageUrl = Faker.Image.PlaceholderUrl(width: 25, height: 25),
                    BarCodeText = Faker.Random.String(length: 10, minChar: '0', maxChar: '9')
                };
                db.Save(orderItem);
                bookedOrderItemInfos.Add(new BookedOrderItemInfo
                {
                    OrderItemId = orderItem.Id,
                    PinCode = orderItem.PinCode,
                    ImageUrl = orderItem.ImageUrl,
                    BarCodeText = orderItem.BarCodeText
                });
            }

            RecalculateSpaces(db, thisOccurrence);
            return (ReserveOrderItemsResult.Success, bookedOrderItemInfos);
        }

        // TODO this should reuse code of LeaseOrderItemsForFacilityOccurrence
        public static (ReserveOrderItemsResult, List<BookedOrderItemInfo>) BookOrderItemsForFacilitySlot(
            FakeDatabaseTransaction transaction,
            string clientId,
            long? sellerId,
            string uuid,
            long slotId,
            string opportunityJsonLdType,
            string opportunityJsonLdId,
            string offerJsonLdId,
            long numberOfSpaces,
            bool proposal
            )
        {
            var db = transaction.DatabaseConnection;
            var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
            var thisFacility = db.Single<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

            if (thisSlot == null || thisFacility == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null);

            if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null);

            if (thisSlot.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisSlot.Start - thisSlot.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            db.Delete<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid && x.SlotId == slotId);
            RecalculateSlotUses(db, thisSlot);

            // Only lease if all spaces requested are available
            if (thisSlot.RemainingUses - thisSlot.LeasedUses < numberOfSpaces)
                return (ReserveOrderItemsResult.NotEnoughCapacity, null);

            var bookedOrderItemInfos = new List<BookedOrderItemInfo>();
            for (var i = 0; i < numberOfSpaces; i++)
            {
                var orderItem = new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid,
                    Status = proposal ? BookingStatus.Proposed : BookingStatus.Confirmed,
                    SlotId = slotId,
                    OpportunityJsonLdType = opportunityJsonLdType,
                    OpportunityJsonLdId = opportunityJsonLdId,
                    OfferJsonLdId = offerJsonLdId,
                    // Include the price locked into the OrderItem as the opportunity price may change
                    Price = thisSlot.Price.Value,
                    PinCode = Faker.Random.String(6, minChar: '0', maxChar: '9'),
                    ImageUrl = Faker.Image.PlaceholderUrl(width: 25, height: 25),
                    BarCodeText = Faker.Random.String(length: 10, minChar: '0', maxChar: '9')
                };

                db.Save(orderItem);

                bookedOrderItemInfos.Add(new BookedOrderItemInfo
                {
                    OrderItemId = orderItem.Id,
                    PinCode = orderItem.PinCode,
                    ImageUrl = orderItem.ImageUrl,
                    BarCodeText = orderItem.BarCodeText
                });
            }

            RecalculateSlotUses(db, thisSlot);
            return (ReserveOrderItemsResult.Success, bookedOrderItemInfos);
        }

        public bool CancelOrderItems(string clientId, long? sellerId, string uuid, List<long> orderItemIds, bool customerCancelled, bool includeCancellationMessage = false)
        {
            using (var db = Mem.Database.Open())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {
                var order = customerCancelled
                    ? db.Single<OrderTable>(x => x.ClientId == clientId && x.OrderMode == OrderMode.Booking && x.OrderId == uuid && !x.Deleted)
                    : db.Single<OrderTable>(x => x.OrderId == uuid && !x.Deleted);

                if (order == null)
                    return false;

                if (sellerId.HasValue && order.SellerId != sellerId)
                    throw new ArgumentException("SellerId does not match Order");

                var whereClause = customerCancelled
                    ? x => x.ClientId == clientId && x.OrderId == order.OrderId && orderItemIds.Contains(x.Id)
                    : (Expression<Func<OrderItemsTable, bool>>)(x => x.OrderId == order.OrderId);
                var query = db.From<OrderItemsTable>()
                              .LeftJoin<OrderItemsTable, SlotTable>()
                              .LeftJoin<OrderItemsTable, OccurrenceTable>()
                              .LeftJoin<OccurrenceTable, ClassTable>()
                              .Where(whereClause);
                var orderItems = db
                    .SelectMulti<OrderItemsTable, SlotTable, OccurrenceTable, ClassTable>(query)
                    .Where(t => t.Item1.Status == BookingStatus.Confirmed || t.Item1.Status == BookingStatus.Attended || t.Item1.Status == BookingStatus.CustomerCancelled)
                    .ToArray();


                var updatedOrderItems = new List<OrderItemsTable>();
                foreach (var (orderItem, slot, occurrence, @class) in orderItems)
                {
                    var now = DateTime.Now;

                    // Customers can only cancel orderItems if within the cancellation window or if full refund is allowed
                    // If it's the seller cancelling, this restriction does not apply.
                    if (customerCancelled)
                    {
                        if (slot.Id != 0 && slot.LatestCancellationBeforeStartDate != null &&
                            slot.Start - slot.LatestCancellationBeforeStartDate < now)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException();
                        }

                        if (occurrence.Id != 0 &&
                            @class?.LatestCancellationBeforeStartDate != null &&
                            occurrence.Start - @class.LatestCancellationBeforeStartDate < now)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException();
                        }
                        if (slot.Id != 0 && slot.AllowCustomerCancellationFullRefund == false)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException();
                        }
                        if (occurrence.Id != 0 &&
                            @class.AllowCustomerCancellationFullRefund == false)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException();
                        }

                        if (orderItem.Status == BookingStatus.CustomerCancelled)
                        {
                            // If the customer has already cancelled this OrderItem, do nothing to maintain idempotency
                            continue;
                        }
                        else
                        {
                            orderItem.Status = BookingStatus.CustomerCancelled;
                            updatedOrderItems.Add(orderItem);
                        }

                    }
                    else
                    {
                        orderItem.Status = BookingStatus.SellerCancelled;
                        updatedOrderItems.Add(orderItem);

                        if (includeCancellationMessage)
                            orderItem.CancellationMessage = "Order cancelled by seller";
                    }

                    db.Save(orderItem);
                }

                // Update the total price and modified date on the Order to update the feed, if something has changed
                // This makes the call idempotent
                if (updatedOrderItems.Count > 0)
                {
                    var totalPrice = db.Select<OrderItemsTable>(x =>
                        x.ClientId == clientId && x.OrderId == order.OrderId &&
                        (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Sum(x => x.Price);

                    order.TotalOrderPrice = totalPrice;
                    order.VisibleInFeed = true;
                    order.Modified = DateTimeOffset.Now.UtcTicks;
                    db.Update(order);

                    // Note an actual implementation would need to handle different opportunity types here
                    // Update the number of spaces available as a result of cancellation
                    RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                    RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                }

                transaction.Commit();
                return true;
            }
        }

        public bool ReplaceOrderOpportunity(string uuid)
        {
            using (var db = Mem.Database.Open())
            {
                var query = db.From<OrderItemsTable>()
                              .Join<OrderTable>()
                              .Where<OrderItemsTable>(x => x.OrderId == uuid)
                              .Where<OrderTable>(x => x.OrderMode != OrderMode.Proposal);
                var orderItemsAndOrder = db.SelectMulti<OrderItemsTable, OrderTable>(query);
                if (!orderItemsAndOrder.Any())
                    return false;
                var order = orderItemsAndOrder.First().Item2;
                var orderItems = orderItemsAndOrder.Select(x => x.Item1);
                // This makes the call idempotent
                if (orderItemsAndOrder.First().Item2.ProposalStatus == ProposalStatus.SellerAccepted)
                    return true;

                var index = Faker.Random.Int(0, orderItemsAndOrder.Count - 1);
                var orderItem = orderItemsAndOrder[index].Item1;

                if (orderItem.SlotId.HasValue)
                {
                    var oldSlotQuery = db.From<SlotTable>()
                                      .Where(s => s.Id == orderItem.SlotId.Value)
                                      .Take(1);
                    var oldSlot = db.Select(oldSlotQuery).Single();

                    var slotQuery = db.From<SlotTable>()
                                      .Where(s => s.Id != orderItem.SlotId.Value && s.Price <= orderItem.Price)
                                      .Take(1);
                    var slot = db.Select(slotQuery).Single();

                    // Hack to replace JSON LD Ids
                    orderItem.OpportunityJsonLdId = orderItem.OpportunityJsonLdId.Replace($"facility-uses/{oldSlot.FacilityUseId}", $"facility-uses/{slot.FacilityUseId}");
                    orderItem.OpportunityJsonLdId = orderItem.OpportunityJsonLdId.Replace($"facility-use-slots/{oldSlot.Id}", $"facility-use-slots/{slot.Id}");
                    orderItem.OfferJsonLdId = orderItem.OfferJsonLdId.Replace($"facility-uses/{oldSlot.FacilityUseId}", $"facility-uses/{slot.FacilityUseId}");
                    orderItem.OfferJsonLdId = orderItem.OfferJsonLdId.Replace($"facility-uses-slots/{oldSlot.Id}", $"facility-uses-slots/{slot.Id}");

                    orderItem.SlotId = slot.Id;
                }
                else if (orderItem.OccurrenceId.HasValue)
                {
                    var oldOccurrenceQuery = db.From<OccurrenceTable>()
                                            .Where<OccurrenceTable>(o => o.Id == orderItem.OccurrenceId.Value)
                                            .Take(1);
                    var oldOccurrence = db.Select(oldOccurrenceQuery).Single();

                    var occurrenceQuery = db.From<OccurrenceTable>()
                                            .Join<ClassTable>()
                                            .Where<OccurrenceTable>(o => o.Id != orderItem.OccurrenceId.Value)
                                            .Where<ClassTable>(c => c.Price <= orderItem.Price)
                                            .Take(1);
                    var occurrence = db.Select(occurrenceQuery).Single();
                    // Hack to replace JSON LD Ids
                    orderItem.OpportunityJsonLdId = orderItem.OpportunityJsonLdId.Replace($"scheduled-sessions/{oldOccurrence.ClassId}", $"scheduled-sessions/{occurrence.ClassId}");
                    orderItem.OpportunityJsonLdId = orderItem.OpportunityJsonLdId.Replace($"events/{orderItem.OccurrenceId}", $"events/{occurrence.Id}");
                    orderItem.OfferJsonLdId = orderItem.OfferJsonLdId.Replace($"session-series/{oldOccurrence.ClassId}", $"session-series/{occurrence.ClassId}");

                    orderItem.OccurrenceId = occurrence.Id;
                }
                else
                {
                    return false;
                }

                db.Update(orderItem);

                order.TotalOrderPrice = orderItems.Sum(x => x.Price); ;
                order.VisibleInFeed = true;
                order.Modified = DateTimeOffset.Now.UtcTicks;
                db.Update(order);

                // Note an actual implementation would need to handle different opportunity types here
                // Update the number of spaces available as a result of cancellation
                RecalculateSpaces(db, orderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                RecalculateSlotUses(db, orderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                return true;
            }
        }

        public bool AcceptOrderProposal(string uuid)
        {
            using (var db = Mem.Database.Open())
            {
                var order = db.Single<OrderTable>(x => x.OrderMode == OrderMode.Proposal && x.OrderId == uuid && !x.Deleted);
                if (order != null)
                {
                    // This makes the call idempotent
                    if (order.ProposalStatus != ProposalStatus.SellerAccepted)
                    {
                        // Update the status and modified date of the OrderProposal to update the feed
                        order.ProposalStatus = ProposalStatus.SellerAccepted;
                        order.VisibleInFeed = true;
                        order.Modified = DateTimeOffset.Now.UtcTicks;
                        db.Update(order);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public FakeDatabaseBookOrderProposalResult BookOrderProposal(string clientId, long? sellerId, string uuid, string proposalVersionUuid)
        {
            using (var db = Mem.Database.Open())
            {
                // Note call is idempotent, so it might already be in the booked state
                var order = db.Single<OrderTable>(x => x.ClientId == clientId && (x.OrderMode == OrderMode.Proposal || x.OrderMode == OrderMode.Booking) && x.OrderId == uuid && !x.Deleted);
                if (order != null)
                {
                    if (sellerId.HasValue && order.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match OrderProposal");
                    }
                    if (order.ProposalVersionId != proposalVersionUuid)
                    {
                        return FakeDatabaseBookOrderProposalResult.OrderProposalVersionOutdated;
                    }
                    if (order.ProposalStatus != ProposalStatus.SellerAccepted)
                    {
                        return FakeDatabaseBookOrderProposalResult.OrderProposalNotAccepted;
                    }
                    List<OrderItemsTable> updatedOrderItems = new List<OrderItemsTable>();
                    foreach (OrderItemsTable orderItem in db.Select<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId))
                    {
                        if (orderItem.Status != BookingStatus.Confirmed)
                        {
                            updatedOrderItems.Add(orderItem);
                            orderItem.Status = BookingStatus.Confirmed;
                            db.Save(orderItem);
                        }
                    }
                    // Update the status and modified date of the OrderProposal to update the feed, if something has changed
                    // This makes the call idempotent
                    if (updatedOrderItems.Count > 0 || order.OrderMode != OrderMode.Booking)
                    {
                        order.OrderMode = OrderMode.Booking;
                        order.VisibleInFeed = true;
                        order.Modified = DateTimeOffset.Now.UtcTicks;
                        db.Update(order);
                        // Note an actual implementation would need to handle different opportunity types here
                        // Update the number of spaces available as a result of cancellation
                        RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                        RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                    }
                    return FakeDatabaseBookOrderProposalResult.OrderSuccessfullyBooked;
                }
                else
                {
                    return FakeDatabaseBookOrderProposalResult.OrderWasNotFound;
                }
            }
        }


        public bool RejectOrderProposal(string clientId, long? sellerId, string uuid, bool customerRejected)
        {
            using (var db = Mem.Database.Open())
            {
                var order = db.Single<OrderTable>(x => (clientId == null || x.ClientId == clientId) && x.OrderMode == OrderMode.Proposal && x.OrderId == uuid && !x.Deleted);
                if (order != null)
                {
                    if (sellerId.HasValue && order.SellerId != sellerId)
                    {
                        throw new ArgumentException("SellerId does not match OrderProposal");
                    }
                    // Update the status and modified date of the OrderProposal to update the feed, if something has changed
                    if (order.ProposalStatus != ProposalStatus.CustomerRejected && order.ProposalStatus != ProposalStatus.SellerRejected)
                    {
                        order.ProposalStatus = customerRejected ? ProposalStatus.CustomerRejected : ProposalStatus.SellerRejected;
                        order.VisibleInFeed = true;
                        order.Modified = DateTimeOffset.Now.UtcTicks;
                        db.Update(order);
                        // Note an actual implementation would need to handle different opportunity types here
                        // Update the number of spaces available as a result of cancellation
                        List<OrderItemsTable> updatedOrderItems = db.Select<OrderItemsTable>(x => (clientId == null || x.ClientId == clientId) && x.OrderId == order.OrderId).ToList();
                        RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                        RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void RecalculateSlotUses(IDbConnection db, SlotTable slot)
        {
            if (slot == null)
                return;

            // Update number of leased spaces remaining for the opportunity
            var leasedUses = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking && x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected && x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected && x.SlotId == slot.Id).Count();
            slot.LeasedUses = leasedUses;

            // Update number of actual spaces remaining for the opportunity
            var totalUsesTaken = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.OrderMode == OrderMode.Booking && x.OccurrenceId == slot.Id && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Count();
            slot.RemainingUses = slot.MaximumUses - totalUsesTaken;

            // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition)
            // TODO: Document this!
            slot.Modified = DateTimeOffset.Now.UtcTicks;
            db.Update(slot);
        }

        public static void RecalculateSlotUses(IDbConnection db, IEnumerable<long> slotIds)
        {
            foreach (var slotId in slotIds)
            {
                var thisSlot = db.Single<SlotTable>(x => x.Id == slotId && !x.Deleted);
                RecalculateSlotUses(db, thisSlot);
            }
        }

        public static void RecalculateSpaces(IDbConnection db, OccurrenceTable occurrence)
        {
            if (occurrence == null)
                return;

            // Update number of leased spaces remaining for the opportunity
            var leasedSpaces = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking && x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected && x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected && x.OccurrenceId == occurrence.Id).Count();
            occurrence.LeasedSpaces = leasedSpaces;

            // Update number of actual spaces remaining for the opportunity
            var totalSpacesTaken = db.LoadSelect<OrderItemsTable>(x => x.OrderTable.OrderMode == OrderMode.Booking && x.OccurrenceId == occurrence.Id && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended)).Count();
            occurrence.RemainingSpaces = occurrence.TotalSpaces - totalSpacesTaken;

            // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition) // TODO: Document this!
            occurrence.Modified = DateTimeOffset.Now.UtcTicks;
            db.Update(occurrence);
        }

        public static void RecalculateSpaces(IDbConnection db, IEnumerable<long> occurrenceIds)
        {
            foreach (var occurrenceId in occurrenceIds)
            {
                var thisOccurrence = db.Single<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                RecalculateSpaces(db, thisOccurrence);
            }
        }

        public static FakeDatabase GetPrepopulatedFakeDatabase()
        {
            var database = new FakeDatabase();
            using (var db = database.Mem.Database.Open())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {
                CreateSellers(db);
                CreateFakeClasses(db);
                CreateFakeFacilitiesAndSlots(db);
                transaction.Commit();
            }
            return database;
        }

        private static void CreateFakeFacilitiesAndSlots(IDbConnection db)
        {
            var opportunitySeeds = GenerateOpportunitySeedDistribution(OpportunityCount);

            var facilities = opportunitySeeds
                .Select(seed => new FacilityUseTable
                {
                    Id = seed.Id,
                    Deleted = false,
                    Name = $"{Faker.Commerce.ProductMaterial()} {Faker.PickRandomParam("Sports Hall", "Swimming Pool Hall", "Running Hall", "Jumping Hall")}",
                    SellerId = Faker.Random.Bool(0.8f) ? Faker.Random.Long(1, 2) : Faker.Random.Long(3, 5), // distribution: 80% 1-2, 20% 3-5
                })
                .ToList();

            int slotId = 0;
            var slots = opportunitySeeds.Select(seed =>
                Enumerable.Range(0, 10)
                    .Select(_ => new
                    {
                        StartDate = seed.RandomStartDate(),
                        TotalUses = Faker.Random.Int(0, 8),
                        Price = decimal.Parse(Faker.Random.Bool() ? "0.00" : Faker.Commerce.Price(0, 20)),
                    })
                    .Select(slot => new SlotTable
                    {
                        FacilityUseId = seed.Id,
                        Id = slotId++,
                        Deleted = false,
                        Start = slot.StartDate,
                        End = slot.StartDate + TimeSpan.FromMinutes(Faker.Random.Int(30, 360)),
                        MaximumUses = slot.TotalUses,
                        RemainingUses = slot.TotalUses,
                        Price = slot.Price,
                        Prepayment = slot.Price == 0
                            ? Faker.Random.Bool() ? RequiredStatusType.Unavailable : (RequiredStatusType?)null
                            : Faker.Random.Bool() ? Faker.Random.Enum<RequiredStatusType>() : (RequiredStatusType?)null,
                        RequiresAttendeeValidation = Faker.Random.Bool(ProportionWithRequiresAttendeeValidation),
                        RequiresApproval = Faker.Random.Bool(),
                        ValidFromBeforeStartDate = seed.RandomValidFromBeforeStartDate(),
                        LatestCancellationBeforeStartDate = RandomLatestCancellationBeforeStartDate(),
                        AllowCustomerCancellationFullRefund = Faker.Random.Bool()
                    })).SelectMany(os => os);

            db.InsertAll(facilities);
            db.InsertAll(slots);
        }

        public static void CreateFakeClasses(IDbConnection db)
        {
            var opportunitySeeds = GenerateOpportunitySeedDistribution(OpportunityCount);

            var classes = opportunitySeeds
                .Select(seed => new
                {
                    seed.Id,
                    Price = decimal.Parse(Faker.Random.Bool() ? "0.00" : Faker.Commerce.Price(0, 20)),
                    ValidFromBeforeStartDate = seed.RandomValidFromBeforeStartDate()
                })
                .Select(@class => new ClassTable
                {
                    Id = @class.Id,
                    Deleted = false,
                    Title = $"{Faker.Commerce.ProductMaterial()} {Faker.PickRandomParam("Yoga", "Zumba", "Walking", "Cycling", "Running", "Jumping")}",
                    Price = @class.Price,
                    Prepayment = @class.Price == 0
                        ? Faker.Random.Bool() ? RequiredStatusType.Unavailable : (RequiredStatusType?)null
                        : Faker.Random.Bool() ? Faker.Random.Enum<RequiredStatusType>() : (RequiredStatusType?)null,
                    RequiresAttendeeValidation = Faker.Random.Bool(ProportionWithRequiresAttendeeValidation),
                    RequiresApproval = Faker.Random.Bool(),
                    LatestCancellationBeforeStartDate = RandomLatestCancellationBeforeStartDate(),
                    SellerId = Faker.Random.Bool(0.8f) ? Faker.Random.Long(1, 2) : Faker.Random.Long(3, 5), // distribution: 80% 1-2, 20% 3-5
                    ValidFromBeforeStartDate = @class.ValidFromBeforeStartDate,
                    AllowCustomerCancellationFullRefund = Faker.Random.Bool()
                })
                .ToList();

            int occurrenceId = 0;
            var occurrences = opportunitySeeds.Select(seed =>
                Enumerable.Range(0, 10)
                    .Select(_ => new
                    {
                        Start = seed.RandomStartDate(),
                        TotalSpaces = Faker.Random.Bool() ? Faker.Random.Int(0, 50) : Faker.Random.Int(0, 3)
                    })
                    .Select(occurrence => new OccurrenceTable
                    {
                        Id = occurrenceId++,
                        ClassId = seed.Id,
                        Deleted = false,
                        Start = occurrence.Start,
                        End = occurrence.Start + TimeSpan.FromMinutes(Faker.Random.Int(30, 360)),
                        TotalSpaces = occurrence.TotalSpaces,
                        RemainingSpaces = occurrence.TotalSpaces
                    })).SelectMany(os => os);

            db.InsertAll(classes);
            db.InsertAll(occurrences);
        }

        public static void CreateSellers(IDbConnection db)
        {
            var sellers = new List<SellerTable>
            {
                new SellerTable { Id = 1, Name = "Acme Fitness Ltd", IsIndividual = false, IsTaxGross = true },
                new SellerTable { Id = 2, Name = "Road Runner Bookcamp Ltd", IsIndividual = false, IsTaxGross = false },
                new SellerTable { Id = 3, Name = "Lorem Fitsum Ltd", IsIndividual = false, IsTaxGross = true },
                new SellerTable { Id = 4, Name = "Coyote Classes Ltd", IsIndividual = false, IsTaxGross = false },
                new SellerTable { Id = 5, Name = "Jane Smith", IsIndividual = true, IsTaxGross = true }
            };

            db.InsertAll(sellers);
        }

        public (int, int) AddClass(
            string testDatasetIdentifier,
            long? sellerId,
            string title,
            decimal? price,
            long totalSpaces,
            bool requiresApproval = false,
            bool? validFromStartDate = null,
            bool? latestCancellationBeforeStartDate = null,
            bool allowCustomerCancellationFullRefund = true,
            RequiredStatusType? prepayment = null,
            bool requiresAttendeeValidation = false,
            decimal locationLat = 0.1m,
            decimal locationLng = 0.1m)

        {
            var startTime = DateTime.Now.AddDays(1);
            var endTime = DateTime.Now.AddDays(1).AddHours(1);

            using (var db = Mem.Database.Open())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {
                var @class = new ClassTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    Title = title,
                    Price = price,
                    Prepayment = prepayment,
                    SellerId = sellerId ?? 1,
                    RequiresApproval = requiresApproval,
                    ValidFromBeforeStartDate = validFromStartDate.HasValue
                        ? TimeSpan.FromHours(validFromStartDate.Value ? 48 : 4)
                        : (TimeSpan?)null,
                    LatestCancellationBeforeStartDate = latestCancellationBeforeStartDate.HasValue
                        ? TimeSpan.FromHours(latestCancellationBeforeStartDate.Value ? 4 : 48)
                        : (TimeSpan?)null,
                    RequiresAttendeeValidation = requiresAttendeeValidation,
                    LocationLat = locationLat,
                    LocationLng = locationLng,
                    AllowCustomerCancellationFullRefund = allowCustomerCancellationFullRefund
                };
                db.Save(@class);

                var occurrence = new OccurrenceTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    ClassId = @class.Id,
                    Start = startTime,
                    End = endTime,
                    TotalSpaces = totalSpaces,
                    RemainingSpaces = totalSpaces
                };
                db.Save(occurrence);

                transaction.Commit();

                return ((int)@class.Id, (int)occurrence.Id);
            }
        }

        public (int, int) AddFacility(
            string testDatasetIdentifier,
            long? sellerId,
            string title,
            decimal? price,
            long totalUses,
            bool requiresApproval = false,
            bool? validFromStartDate = null,
            bool? latestCancellationBeforeStartDate = null,
            bool allowCustomerCancellationFullRefund = true,
            RequiredStatusType? prepayment = null,
            bool requiresAttendeeValidation = false,
            decimal locationLat = 0.1m,
            decimal locationLng = 0.1m)
        {
            var startTime = DateTime.Now.AddDays(1);
            var endTime = DateTime.Now.AddDays(1).AddHours(1);

            using (var db = Mem.Database.Open())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {
                var facility = new FacilityUseTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    Name = title,
                    SellerId = sellerId ?? 1,
                    LocationLat = locationLat,
                    LocationLng = locationLng,
                };
                db.Save(facility);

                var slot = new SlotTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    FacilityUseId = facility.Id,
                    Start = startTime,
                    End = endTime,
                    MaximumUses = totalUses,
                    RemainingUses = totalUses,
                    Price = price,
                    Prepayment = prepayment,
                    RequiresApproval = requiresApproval,
                    ValidFromBeforeStartDate = validFromStartDate.HasValue
                        ? TimeSpan.FromHours(validFromStartDate.Value ? 48 : 4)
                        : (TimeSpan?)null,
                    LatestCancellationBeforeStartDate = latestCancellationBeforeStartDate.HasValue
                        ? TimeSpan.FromHours(latestCancellationBeforeStartDate.Value ? 4 : 48)
                        : (TimeSpan?)null,
                    RequiresAttendeeValidation = requiresAttendeeValidation,
                    AllowCustomerCancellationFullRefund = allowCustomerCancellationFullRefund
                };
                db.Save(slot);

                transaction.Commit();

                return ((int)facility.Id, (int)slot.Id);
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
        private static readonly (Bounds, Bounds?)[] BucketDefinitions =
        {
            // in next 0-10 days, no validFromBeforeStartDate
            (new Bounds(0, 10), null),
            (new Bounds(0, 10), null),
            // in next 0-10 days, validFromBeforeStartDate between 10-15 days (all bookable)
            (new Bounds(0, 10), new Bounds(10, 15)),
            (new Bounds(0, 10), new Bounds(10, 15)),
            // in next -2-+6 days, validFromBeforeStartDate 0-4 days (over half likely bookable, some likely bookable but in the past)
            (new Bounds(-2, 6), new Bounds(0, 4)),
            // in next 5-10 days, validFromBeforeStartDate between 0-4 days (none bookable)
            (new Bounds(5, 10), new Bounds(0, 4)),
        };

        private static OpportunitySeed GenerateRandomOpportunityData(Faker faker, int index, (Bounds startDateRange, Bounds? validFromBeforeStartDateRange) input)
        {
            return new OpportunitySeed
            {
                Faker = faker,
                Id = index + 1,
                StartDateBounds = BoundsDaysToMinutes(input.startDateRange).Value,
                ValidFromBeforeStartDateBounds = !input.validFromBeforeStartDateRange.HasValue ? (Bounds?)null : BoundsDaysToMinutes(input.validFromBeforeStartDateRange).Value,
            };
        }

        private static Bounds? BoundsDaysToMinutes(Bounds? bounds)
        {
            const int MINUTES_IN_DAY = 60 * 24;
            return !bounds.HasValue ? (Bounds?)null : new Bounds(bounds.Value.Lower * MINUTES_IN_DAY, bounds.Value.Upper * MINUTES_IN_DAY);
        }

        /// <summary>
        /// Used to generate random data.
        /// </summary>
        private static List<OpportunitySeed> GenerateOpportunitySeedDistribution(int count)
        {
            return Faker.GenerateIntegerDistribution(count, BucketDefinitions, GenerateRandomOpportunityData).ToList();
        }

        private struct OpportunitySeed
        {
            public Faker Faker { get; set; }
            public int Id { get; set; }
            public Bounds StartDateBounds { get; set; }
            public Bounds? ValidFromBeforeStartDateBounds { get; set; }

            public DateTime RandomStartDate() => DateTime.Now.AddMinutes(this.Faker.Random.Int(StartDateBounds));

            public TimeSpan? RandomValidFromBeforeStartDate() => ValidFromBeforeStartDateBounds.HasValue
                ? TimeSpan.FromMinutes(this.Faker.Random.Int(ValidFromBeforeStartDateBounds.Value))
                : (TimeSpan?)null;
        }

        private static TimeSpan? RandomLatestCancellationBeforeStartDate()
        {
            if (Faker.Random.Bool(1f / 3))
                return null;

            return Faker.Random.Bool() ? TimeSpan.FromDays(1) : TimeSpan.FromHours(40);
        }
    }
}
