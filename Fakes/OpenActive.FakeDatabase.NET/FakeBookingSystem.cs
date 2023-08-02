using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Linq.Expressions;
using Bogus;
using OpenActive.FakeDatabase.NET.Helpers;
using ServiceStack.OrmLite;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.OrmLite.Dapper;
using OpenActive.NET;

namespace OpenActive.FakeDatabase.NET
{
    /// <summary>
    /// This class models the database schema within an actual booking system.
    /// It is designed to simulate the database that woFuld be available in a full implementation.
    /// </summary>
    public class FakeBookingSystem
    {
        public FakeDatabase Database { get; set; }
        public FakeBookingSystem(bool facilityUseHasSlots)
        {
            Database = FakeDatabase.GetPrepopulatedFakeDatabase(facilityUseHasSlots).Result;

        }
    }

    /// <summary>
    /// Extension methods for hashing strings and byte arrays
    /// </summary>
    internal static class HashExtensions
    {
        /// <summary>
        /// Creates a SHA256 hash of the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A hash</returns>
        public static string Sha256(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
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
            var connectionString = Path.GetTempPath() + "openactive-fakedatabase.db";
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

            // OrmLiteUtils.PrintSql();
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
    /// Result of getting (or attempting to get) an Order in a FakeDatabase
    /// </summary>
    public enum FakeDatabaseGetOrderResult
    {
        OrderSuccessfullyGot,
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

    public enum CustomerType
    {
        Organization,
        Person,
        None
    }
    public class BookingPartnerAdministratorTable
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; } = true;
        public List<Claim> Claims { get; set; }
    }

    public class FakeDatabase
    {
        private const float ProportionWithRequiresAttendeeValidation = 1f / 10;
        private const float ProportionWithRequiresAdditionalDetails = 1f / 10;
        private bool _facilityUseHasSlots;
        public readonly InMemorySQLite Mem = new InMemorySQLite();

        private static readonly Faker Faker = new Faker();

        public FakeDatabase(bool facilityUseHasSlots)
        {
            _facilityUseHasSlots = facilityUseHasSlots;
        }

        static FakeDatabase()
        {
            Randomizer.Seed = new Random((int)(DateTime.Today - new DateTime(1970, 1, 1)).TotalDays);
        }

        private static readonly int OpportunityCount =
            int.TryParse(Environment.GetEnvironmentVariable("OPPORTUNITY_COUNT"), out var opportunityCount) ? opportunityCount : 2000;

        /// <summary>
        /// TODO: Call this on a schedule from both .NET Core and .NET Framework reference implementations
        /// </summary>
        public async Task CleanupExpiredLeases()
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var occurrenceIds = new List<long>();
                var slotIds = new List<long>();

                foreach (var order in await db.SelectAsync<OrderTable>(x => x.LeaseExpires < DateTimeOffset.Now))
                {
                    // ReSharper disable twice PossibleInvalidOperationException
                    occurrenceIds.AddRange((await db.SelectAsync<OrderItemsTable>(x => x.OrderId == order.OrderId && x.OccurrenceId.HasValue)).Select(x => x.OccurrenceId.Value));
                    slotIds.AddRange((await db.SelectAsync<OrderItemsTable>(x => x.OrderId == order.OrderId && x.SlotId.HasValue)).Select(x => x.SlotId.Value));
                    await db.DeleteAsync<OrderItemsTable>(x => x.OrderId == order.OrderId);
                    await db.DeleteAsync<OrderTable>(x => x.OrderId == order.OrderId);
                }

                await RecalculateSpaces(db, occurrenceIds.Distinct());
                await RecalculateSlotUses(db, slotIds.Distinct());
            }
        }

        public static async Task<bool> AddLease(string clientId, Guid uuid, BrokerRole brokerRole, string brokerName, Uri brokerUrl, string brokerTelephone, long? sellerId, string customerEmail, DateTimeOffset leaseExpires, FakeDatabaseTransaction transaction)
        {
            var db = transaction.DatabaseConnection;

            var existingOrder = await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString());
            if (existingOrder == null)
            {
                await db.InsertAsync(new OrderTable
                {
                    ClientId = clientId,
                    OrderId = uuid.ToString(),
                    Deleted = false,
                    BrokerRole = brokerRole,
                    BrokerName = brokerName,
                    BrokerUrl = brokerUrl,
                    BrokerTelephone = brokerTelephone,
                    SellerId = sellerId ?? 1,
                    CustomerEmail = customerEmail,
                    OrderMode = OrderMode.Lease,
                    LeaseExpires = leaseExpires.DateTime,
                    VisibleInOrdersFeed = FeedVisibility.None
                });

                return true;
            }

            // Return false if there's a clash with an existing Order or OrderProposal
            if (existingOrder.OrderMode != OrderMode.Lease || existingOrder.Deleted)
                return false;

            // Reuse existing lease if it exists
            existingOrder.BrokerRole = brokerRole;
            existingOrder.BrokerName = brokerName;
            existingOrder.BrokerUrl = brokerUrl;
            existingOrder.BrokerTelephone = brokerTelephone;
            existingOrder.SellerId = sellerId ?? 1;
            existingOrder.CustomerEmail = customerEmail;
            existingOrder.OrderMode = OrderMode.Lease;
            existingOrder.LeaseExpires = leaseExpires.DateTime;
            await db.UpdateAsync(existingOrder);

            // TODO: Remove this and improve leasing logic to add/update rather than delete/replace
            // Remove previous lease
            await db.DeleteAsync<OrderItemsTable>(x => x.OrderId == existingOrder.OrderId);
            return true;
        }

        /// <summary>
        /// Update logistics data for FacilityUse to trigger logistics change notification
        /// </summary>
        /// <param name="slotId"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public async Task<bool> UpdateFacilityUseName(long slotId, string newName)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<SlotTable>()
                              .LeftJoin<SlotTable, FacilityUseTable>()
                              .Where(x => x.Id == slotId)
                              .And<FacilityUseTable>(y => !y.Deleted);
                var facilityUse = (await db.SelectAsync<FacilityUseTable>(query)).Single();
                if (facilityUse == null)
                    return false;

                facilityUse.Name = newName;
                facilityUse.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(facilityUse);
                return true;
            }
        }

        /// <summary>
        /// Update logistics data for Slot to trigger logistics change notification
        /// </summary>
        /// <param name="slotId"></param>
        /// <param name="numberOfMins"></param>
        /// <returns></returns>
        public async Task<bool> UpdateFacilitySlotStartAndEndTimeByPeriodInMins(long slotId, int numberOfMins)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var slot = await db.SingleAsync<SlotTable>(x => x.Id == slotId && !x.Deleted);
                if (slot == null)
                    return false;

                slot.Start = slot.Start.AddMinutes(numberOfMins);
                slot.End = slot.End.AddMinutes(numberOfMins);
                slot.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(slot);
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
        public async Task<bool> UpdateFacilityUseLocationPlaceId(long slotId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<SlotTable>()
                              .LeftJoin<SlotTable, FacilityUseTable>()
                              .Where(x => x.Id == slotId)
                              .And<FacilityUseTable>(y => !y.Deleted);
                var facilityUse = (await db.SelectAsync<FacilityUseTable>(query)).Single();
                if (facilityUse == null)
                    return false;

                // Round-robin to a different place
                facilityUse.PlaceId = (facilityUse.PlaceId + 1) % 3 + 1;
                facilityUse.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(facilityUse);
                return true;
            }
        }

        /// <summary>
        /// Update name logistics data for SessionSeries to trigger logistics change notification
        /// </summary>
        /// <param name="occurrenceId"></param>
        /// <param name="newTitle"></param>
        /// <returns></returns>
        public async Task<bool> UpdateClassTitle(long occurrenceId, string newTitle)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<OccurrenceTable>()
                            .LeftJoin<OccurrenceTable, ClassTable>()
                            .Where(x => x.Id == occurrenceId)
                            .And<ClassTable>(y => !y.Deleted);
                var classInstance = await db.SingleAsync<ClassTable>(query);
                if (classInstance == null)
                    return false;

                classInstance.Title = newTitle;
                classInstance.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(classInstance);
                return true;
            }
        }

        /// <summary>
        /// Update time based logistics data for ScheduledSession to trigger logistics change notification
        /// </summary>
        /// <param name="occurrenceId"></param>
        /// <param name="numberOfMins"></param>
        /// <returns></returns>
        public async Task<bool> UpdateScheduledSessionStartAndEndTimeByPeriodInMins(long occurrenceId, int numberOfMins)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var occurrence = await db.SingleAsync<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                if (occurrence == null)
                    return false;

                occurrence.Start = occurrence.Start.AddMinutes(numberOfMins);
                occurrence.End = occurrence.End.AddMinutes(numberOfMins);
                occurrence.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(occurrence);
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
        public async Task<bool> UpdateSessionSeriesLocationPlaceId(long occurrenceId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<OccurrenceTable>()
                            .LeftJoin<OccurrenceTable, ClassTable>()
                            .Where(x => x.Id == occurrenceId)
                            .And<ClassTable>(y => !y.Deleted);
                var classInstance = await db.SingleAsync<ClassTable>(query);
                if (classInstance == null)
                    return false;

                // Round-robin to a different place
                classInstance.PlaceId = (classInstance.PlaceId + 1) % 3 + 1;
                classInstance.Modified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(classInstance);
                return true;
            }
        }

        public async Task<bool> UpdateAccess(Guid uuid, bool updateAccessPass = false, bool updateAccessCode = false, bool updateAccessChannel = false)
        {
            if (!updateAccessPass && !updateAccessCode && !updateAccessChannel)
                return false;

            using (var db = await Mem.Database.OpenAsync())
            {
                OrderTable order = await db.SingleAsync<OrderTable>(x => x.OrderId == uuid.ToString() && !x.Deleted);

                if (order != null)
                {
                    List<OrderItemsTable> orderItems = await db.SelectAsync<OrderItemsTable>(x => x.OrderId == order.OrderId);

                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.None)
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

                            if (updateAccessChannel)
                            {
                                orderItem.MeetingUrl = new Uri(Faker.Internet.Url());
                                orderItem.MeetingId = Faker.Random.String(length: 10, minChar: '0', maxChar: '9');
                                orderItem.MeetingPassword = Faker.Random.String(length: 10, minChar: '0', maxChar: '9');
                            }

                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            await db.SaveAsync(orderItem);
                        }
                    }

                    order.OrderModified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInOrdersFeed = FeedVisibility.Visible;
                    await db.UpdateAsync(order);

                    return true;
                }

                return false;
            }
        }

        public async Task<bool> UpdateOpportunityAttendance(Guid uuid, bool attended)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                OrderTable order = await db.SingleAsync<OrderTable>(x => x.OrderId == uuid.ToString() && !x.Deleted);

                if (order != null)
                {
                    List<OrderItemsTable> orderItems = await db.SelectAsync<OrderItemsTable>(x => x.OrderId == order.OrderId);

                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.None)
                        {
                            orderItem.Status = attended ? BookingStatus.Attended : BookingStatus.Absent;
                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            await db.UpdateAsync(orderItem);
                        }
                    }

                    order.OrderModified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInOrdersFeed = FeedVisibility.Visible;
                    await db.UpdateAsync(order);

                    return true;
                }

                return false;
            }
        }

        public async Task<bool> AddCustomerNotice(Guid uuid)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                OrderTable order = await db.SingleAsync<OrderTable>(x => x.OrderId == uuid.ToString() && !x.Deleted);
                if (order != null)
                {
                    List<OrderItemsTable> orderItems = await db.SelectAsync<OrderItemsTable>(x => x.OrderId == order.OrderId);
                    foreach (OrderItemsTable orderItem in orderItems)
                    {
                        if (orderItem.Status == BookingStatus.Confirmed || orderItem.Status == BookingStatus.None)
                        {
                            orderItem.CustomerNotice = $"customer notice message: {Faker.Random.String(10, minChar: 'a', maxChar: 'z')}";
                            orderItem.Modified = DateTimeOffset.Now.UtcTicks;
                            await db.UpdateAsync(orderItem);
                        }
                    }

                    order.OrderModified = DateTimeOffset.Now.UtcTicks;
                    order.VisibleInOrdersFeed = FeedVisibility.Visible;
                    await db.UpdateAsync(order);

                    return true;
                }

                return false;
            }
        }

        public async Task DeleteBookingPartner(string clientId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.DeleteAsync<BookingPartnerTable>(x => x.ClientId == clientId);
            }
        }

        public async Task DeleteLease(string clientId, Guid uuid, long? sellerId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                // TODO: Note this should throw an error if the Seller ID does not match, same as DeleteOrder
                if (await db.ExistsAsync<OrderTable>(x => x.ClientId == clientId && x.OrderMode == OrderMode.Lease && x.OrderId == uuid.ToString() && (!sellerId.HasValue || x.SellerId == sellerId)))
                {
                    // ReSharper disable twice PossibleInvalidOperationException
                    var occurrenceIds = (await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.OccurrenceId.HasValue)).Select(x => x.OccurrenceId.Value).Distinct();
                    var slotIds = (await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.SlotId.HasValue)).Select(x => x.SlotId.Value).Distinct();

                    await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString());
                    await db.DeleteAsync<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString());

                    await RecalculateSpaces(db, occurrenceIds);
                    await RecalculateSlotUses(db, slotIds);
                }
            }
        }

        public static async Task<bool> AddOrder(
            string clientId, Guid uuid, BrokerRole brokerRole, string brokerName, Uri brokerUrl, string brokerTelephone, long? sellerId,
            string customerEmail, CustomerType customerType, string customerOrganizationName,
            string customerIdentifier, string customerGivenName, string customerFamilyName, string customerTelephone,
            string paymentIdentifier, string paymentName, string paymentProviderId, string paymentAccountId,
            decimal totalOrderPrice, FakeDatabaseTransaction transaction, Guid? proposalVersionUuid, ProposalStatus? proposalStatus)
        {
            var db = transaction.DatabaseConnection;

            var existingOrder = await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString());
            if (existingOrder == null)
            {
                await db.InsertAsync(new OrderTable
                {
                    ClientId = clientId,
                    OrderId = uuid.ToString(),
                    Deleted = false,
                    BrokerRole = brokerRole,
                    BrokerName = brokerName,
                    BrokerUrl = brokerUrl,
                    BrokerTelephone = brokerTelephone,
                    SellerId = sellerId ?? 1,
                    CustomerEmail = customerEmail,
                    CustomerType = customerType,
                    CustomerOrganizationName = customerOrganizationName,
                    CustomerIdentifier = customerIdentifier,
                    CustomerGivenName = customerGivenName,
                    CustomerFamilyName = customerFamilyName,
                    CustomerTelephone = customerTelephone,
                    PaymentIdentifier = paymentIdentifier,
                    PaymentName = paymentName,
                    PaymentProviderId = paymentProviderId,
                    PaymentAccountId = paymentAccountId,
                    TotalOrderPrice = totalOrderPrice,
                    OrderMode = proposalVersionUuid != null ? OrderMode.Proposal : OrderMode.Booking,
                    VisibleInOrdersFeed = FeedVisibility.None,
                    ProposalVersionId = proposalVersionUuid,
                    ProposalStatus = proposalStatus
                });
                return true;
            }
            // Return false if there's a clash with an existing Order or OrderProposal

            if (existingOrder.OrderMode != OrderMode.Lease || existingOrder.Deleted)
            {
                return false;
            }
            // Reuse existing lease if it exists
            existingOrder.BrokerRole = brokerRole;
            existingOrder.BrokerName = brokerName;
            existingOrder.BrokerUrl = brokerUrl;
            existingOrder.BrokerTelephone = brokerTelephone;
            existingOrder.SellerId = sellerId ?? 1;
            existingOrder.CustomerEmail = customerEmail;
            existingOrder.CustomerType = customerType;
            existingOrder.CustomerOrganizationName = customerOrganizationName;
            existingOrder.CustomerIdentifier = customerIdentifier;
            existingOrder.CustomerGivenName = customerGivenName;
            existingOrder.CustomerFamilyName = customerFamilyName;
            existingOrder.CustomerTelephone = customerTelephone;
            existingOrder.PaymentIdentifier = paymentIdentifier;
            existingOrder.PaymentName = paymentName;
            existingOrder.PaymentProviderId = paymentProviderId;
            existingOrder.PaymentAccountId = paymentAccountId;
            existingOrder.TotalOrderPrice = totalOrderPrice;
            existingOrder.OrderMode = proposalVersionUuid != null ? OrderMode.Proposal : OrderMode.Booking;
            existingOrder.ProposalVersionId = proposalVersionUuid;
            existingOrder.ProposalStatus = proposalStatus;
            await db.UpdateAsync(existingOrder);

            return true;
        }

        public async Task<(FakeDatabaseGetOrderResult, OrderTable, List<OrderItemsTable>)> GetOrderAndOrderItems(string clientId, long? sellerId, Guid uuid)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var order = await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && !x.Deleted && (!sellerId.HasValue || x.SellerId == sellerId));
                if (order == null) return (FakeDatabaseGetOrderResult.OrderWasNotFound, null, null);
                var orderItems = await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString());
                if (orderItems.Count == 0) return (FakeDatabaseGetOrderResult.OrderWasNotFound, null, null);

                return (FakeDatabaseGetOrderResult.OrderSuccessfullyGot, order, orderItems);
            }
        }

        public async Task<(bool, ClassTable, OccurrenceTable, BookedOrderItemInfo)> GetOccurrenceAndBookedOrderItemInfoByOccurrenceId(Guid uuid, long? occurrenceId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<OccurrenceTable>()
                    .LeftJoin<OccurrenceTable, ClassTable>()
                    .Where(x => x.Id == occurrenceId);
                var rows = db.SelectMulti<OccurrenceTable, ClassTable>(query);
                if (!rows.Any())
                    return (true, null, null, null);

                var (occurrence, thisClass) = rows.FirstOrDefault();

                var orderItem = await db.SingleAsync<OrderItemsTable>(x => x.OrderId == uuid.ToString() && x.OccurrenceId == occurrenceId);
                var bookedOrderItemInfo = orderItem != null && orderItem.Status == BookingStatus.Confirmed ?
                     new BookedOrderItemInfo
                     {
                         OrderItemId = orderItem.Id,
                         PinCode = orderItem.PinCode,
                         ImageUrl = orderItem.ImageUrl,
                         BarCodeText = orderItem.BarCodeText,
                         MeetingId = orderItem.MeetingId,
                         MeetingPassword = orderItem.MeetingPassword,
                         AttendanceMode = thisClass.AttendanceMode,
                     }
                     : null;

                return (
                    true,
                    thisClass,
                    occurrence,
                    bookedOrderItemInfo
                );
            }
        }

        public OpenActive.NET.Place GetPlaceById(long placeId)
        {
            // Three hardcoded fake places
            switch (placeId)
            {
                case 1:
                    return new OpenActive.NET.Place
                    {
                        Identifier = 1,
                        Name = "Post-ercise Plaza",
                        Description = "Sorting Out Your Fitness One Parcel Lift at a Time! Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new OpenActive.NET.PostalAddress
                        {
                            StreetAddress = "Kings Mead House",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1AA",
                            AddressCountry = "GB"
                        },
                        Geo = new OpenActive.NET.GeoCoordinates
                        {
                            Latitude = (decimal?)51.7502,
                            Longitude = (decimal?)-1.2674
                        },
                        Image = new List<OpenActive.NET.ImageObject> {
                            new OpenActive.NET.ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/e/e5/Oxford_StAldates_PostOffice.jpg")
                            },
                        },
                        Telephone = "01865 000001",
                        Url = new Uri("https://en.wikipedia.org/wiki/Post_Office_Limited"),
                        AmenityFeature = new List<OpenActive.NET.LocationFeatureSpecification>
                        {
                            new OpenActive.NET.ChangingFacilities { Name = "Changing Facilities", Value = true },
                            new OpenActive.NET.Showers { Name = "Showers", Value = true },
                            new OpenActive.NET.Lockers { Name = "Lockers", Value = true },
                            new OpenActive.NET.Towels { Name = "Towels", Value = false },
                            new OpenActive.NET.Creche { Name = "Creche", Value = false },
                            new OpenActive.NET.Parking { Name = "Parking", Value = false }
                        }
                    };
                case 2:
                    return new OpenActive.NET.Place
                    {
                        Identifier = 2,
                        Name = "Premier Lifters",
                        Description = "Where your Fitness Goals are Always Inn-Sight. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new OpenActive.NET.PostalAddress
                        {
                            StreetAddress = "Greyfriars Court, Paradise Square",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1BB",
                            AddressCountry = "GB"
                        },
                        Geo = new OpenActive.NET.GeoCoordinates
                        {
                            Latitude = (decimal?)51.7504933,
                            Longitude = (decimal?)-1.2620685
                        },
                        Image = new List<OpenActive.NET.ImageObject> { 
                            new OpenActive.NET.ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/5/53/Cambridge_Orchard_Park_Premier_Inn.jpg")
                            },
                        },
                        Telephone = "01865 000002",
                        Url = new Uri("https://en.wikipedia.org/wiki/Premier_Inn"),
                        AmenityFeature = new List<OpenActive.NET.LocationFeatureSpecification>
                        {
                            new OpenActive.NET.ChangingFacilities { Name = "Changing Facilities", Value = false },
                            new OpenActive.NET.Showers { Name = "Showers", Value = false },
                            new OpenActive.NET.Lockers { Name = "Lockers", Value = false },
                            new OpenActive.NET.Towels { Name = "Towels", Value = true },
                            new OpenActive.NET.Creche { Name = "Creche", Value = true },
                            new OpenActive.NET.Parking { Name = "Parking", Value = true }
                        }
                    };
                case 3:
                    return new OpenActive.NET.Place
                    {
                        Identifier = 3,
                        Name = "Stroll & Stretch",
                        Description = "Casual Calisthenics in the Heart of Commerce. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                        Address = new OpenActive.NET.PostalAddress
                        {
                            StreetAddress = "Norfolk Street",
                            AddressLocality = "Oxford",
                            AddressRegion = "Oxfordshire",
                            PostalCode = "OX1 1UU",
                            AddressCountry = "GB"
                        },
                        Geo = new OpenActive.NET.GeoCoordinates
                        {
                            Latitude = (decimal?)51.749826,
                            Longitude = (decimal?)-1.261492
                        },
                        Image = new List<OpenActive.NET.ImageObject> { 
                            new OpenActive.NET.ImageObject
                            {
                                Url = new Uri("https://upload.wikimedia.org/wikipedia/commons/2/28/Westfield_Garden_State_Plaza_-_panoramio.jpg")
                            },
                        },
                        Telephone = "01865 000003",
                        Url = new Uri("https://en.wikipedia.org/wiki/Shopping_center"),
                    };
                default:
                    return null;
            }
        }

        public async Task<(bool, FacilityUseTable, SlotTable, BookedOrderItemInfo)> GetSlotAndBookedOrderItemInfoBySlotId(Guid uuid, long? slotId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<SlotTable>()
                    .LeftJoin<SlotTable, FacilityUseTable>()
                    .Where(x => x.Id == slotId);
                var rows = db.SelectMulti<SlotTable, FacilityUseTable>(query);
                if (!rows.Any())
                    return (false, null, null, null);

                var (slot, facilityUse) = rows.FirstOrDefault();
                var orderItem = await db.SingleAsync<OrderItemsTable>(x => x.OrderId == uuid.ToString() && x.SlotId == slotId);
                var bookedOrderItemInfo = (orderItem != null && orderItem.Status == BookingStatus.Confirmed) ?
                     new BookedOrderItemInfo
                     {
                         OrderItemId = orderItem.Id,
                         PinCode = orderItem.PinCode,
                         ImageUrl = orderItem.ImageUrl,
                         BarCodeText = orderItem.BarCodeText,
                     }
                     : null;

                return (
                    true,
                    facilityUse,
                    slot,
                    bookedOrderItemInfo
                );
            }
        }

        public async Task<FakeDatabaseDeleteOrderResult> DeleteOrder(string clientId, Guid uuid, long? sellerId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                // Set the Order to deleted in the feed, and erase all associated personal data
                var order = await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.OrderMode != OrderMode.Lease);
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
                order.OrderModified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(order);

                var occurrenceIds = (await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId && x.OccurrenceId.HasValue)).Select(x => x.OccurrenceId.Value).Distinct();
                var slotIds = (await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.SlotId.HasValue)).Select(x => x.SlotId.Value).Distinct();
                await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId);

                await RecalculateSpaces(db, occurrenceIds);
                await RecalculateSlotUses(db, slotIds);

                return FakeDatabaseDeleteOrderResult.OrderSuccessfullyDeleted;
            }
        }

        public static async Task<(ReserveOrderItemsResult, long?, long?)> LeaseOrderItemsForClassOccurrence(FakeDatabaseTransaction transaction, string clientId, long? sellerId, Guid uuid, long occurrenceId, long spacesRequested)
        {
            var db = transaction.DatabaseConnection;
            var thisOccurrence = await db.SingleAsync<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
            var thisClass = await db.SingleAsync<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

            if (thisOccurrence == null || thisClass == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null, null);

            if (sellerId.HasValue && thisClass.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null, null);

            if (thisClass.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisOccurrence.Start - thisClass.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.OccurrenceId == occurrenceId);
            await RecalculateSpaces(db, thisOccurrence);

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
                await db.InsertAsync(new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid.ToString(),
                    OccurrenceId = occurrenceId,
                    Status = BookingStatus.None
                });
            }

            // Update number of spaces remaining for the opportunity
            await RecalculateSpaces(db, thisOccurrence);
            return (ReserveOrderItemsResult.Success, null, null);
        }

        public static async Task<(ReserveOrderItemsResult, long?, long?)> LeaseOrderItemsForFacilitySlot(FakeDatabaseTransaction transaction, string clientId, long? sellerId, Guid uuid, long slotId, long spacesRequested)
        {
            var db = transaction.DatabaseConnection;
            var thisSlot = await db.SingleAsync<SlotTable>(x => x.Id == slotId && !x.Deleted);
            var thisFacility = await db.SingleAsync<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

            if (thisSlot == null || thisFacility == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null, null);

            if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null, null);

            if (thisSlot.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisSlot.Start - thisSlot.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.SlotId == slotId);
            await RecalculateSlotUses(db, thisSlot);

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
                await db.InsertAsync(new OrderItemsTable
                {
                    ClientId = clientId,
                    Deleted = false,
                    OrderId = uuid.ToString(),
                    SlotId = slotId,
                    Status = BookingStatus.None
                });
            }

            // Update number of spaces remaining for the opportunity
            await RecalculateSlotUses(db, thisSlot);
            return (ReserveOrderItemsResult.Success, null, null);
        }

        public class BookedOrderItemInfo
        {
            public long OrderItemId { get; set; }
            public string PinCode { get; set; }
            public string ImageUrl { get; set; }
            public string BarCodeText { get; set; }
            public Uri MeetingUrl { get; set; }
            public string MeetingId { get; set; }
            public string MeetingPassword { get; set; }
            public AttendanceMode AttendanceMode { get; set; }
        }

        // TODO this should reuse code of LeaseOrderItemsForClassOccurrence
        public static async Task<(ReserveOrderItemsResult, List<BookedOrderItemInfo>)> BookOrderItemsForClassOccurrence(
            FakeDatabaseTransaction transaction,
            string clientId,
            long? sellerId,
            Guid uuid,
            long occurrenceId,
            Uri opportunityJsonLdId,
            Uri offerJsonLdId,
            long numberOfSpaces,
            bool proposal,
            List<string> attendees,
            List<string> additionalDetailsString
            )
        {
            var db = transaction.DatabaseConnection;
            var thisOccurrence = await db.SingleAsync<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
            var thisClass = await db.SingleAsync<ClassTable>(x => x.Id == thisOccurrence.ClassId && !x.Deleted);

            if (thisOccurrence == null || thisClass == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null);

            if (sellerId.HasValue && thisClass.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null);

            if (thisClass.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisOccurrence.Start - thisClass.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.OccurrenceId == occurrenceId);
            await RecalculateSpaces(db, thisOccurrence);

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
                    OrderId = uuid.ToString(),
                    Status = proposal ? BookingStatus.None : BookingStatus.Confirmed,
                    OccurrenceId = occurrenceId,
                    OpportunityJsonLdId = opportunityJsonLdId,
                    OfferJsonLdId = offerJsonLdId,
                    // Include the price locked into the OrderItem as the opportunity price may change
                    Price = thisClass.Price.Value,
                    PinCode = thisClass.AttendanceMode != AttendanceMode.Online ? Faker.Random.String(length: 6, minChar: '0', maxChar: '9') : null,
                    ImageUrl = thisClass.AttendanceMode != AttendanceMode.Online ? Faker.Image.PlaceholderUrl(width: 25, height: 25) : null,
                    BarCodeText = thisClass.AttendanceMode != AttendanceMode.Online ? Faker.Random.String(length: 10, minChar: '0', maxChar: '9') : null,
                    MeetingUrl = thisClass.AttendanceMode != AttendanceMode.Offline ? new Uri(Faker.Internet.Url()) : null,
                    MeetingId = thisClass.AttendanceMode != AttendanceMode.Offline ? Faker.Random.String(length: 10, minChar: '0', maxChar: '9') : null,
                    MeetingPassword = thisClass.AttendanceMode != AttendanceMode.Offline ? Faker.Random.String(length: 10, minChar: '0', maxChar: '9') : null,
                    AttendeeString = attendees.Count > i ? attendees[i] : null,
                    AdditionalDetailsString = additionalDetailsString.Count > i ? additionalDetailsString[i] : null,
                };

                await db.SaveAsync(orderItem);
                bookedOrderItemInfos.Add(new BookedOrderItemInfo
                {
                    OrderItemId = orderItem.Id,
                    PinCode = orderItem.PinCode,
                    ImageUrl = orderItem.ImageUrl,
                    BarCodeText = orderItem.BarCodeText,
                    MeetingId = orderItem.MeetingId,
                    MeetingPassword = orderItem.MeetingPassword,
                    AttendanceMode = thisClass.AttendanceMode,
                });
            }

            await RecalculateSpaces(db, thisOccurrence);
            return (ReserveOrderItemsResult.Success, bookedOrderItemInfos);
        }

        // TODO this should reuse code of LeaseOrderItemsForFacilityOccurrence
        public static async Task<(ReserveOrderItemsResult, List<BookedOrderItemInfo>)> BookOrderItemsForFacilitySlot(
            FakeDatabaseTransaction transaction,
            string clientId,
            long? sellerId,
            Guid uuid,
            long slotId,
            Uri opportunityJsonLdId,
            Uri offerJsonLdId,
            long numberOfSpaces,
            bool proposal,
            List<string> attendees,
            List<string> additionalDetailsString
            )
        {
            var db = transaction.DatabaseConnection;
            var thisSlot = await db.SingleAsync<SlotTable>(x => x.Id == slotId && !x.Deleted);
            var thisFacility = await db.SingleAsync<FacilityUseTable>(x => x.Id == thisSlot.FacilityUseId && !x.Deleted);

            if (thisSlot == null || thisFacility == null)
                return (ReserveOrderItemsResult.OpportunityNotFound, null);

            if (sellerId.HasValue && thisFacility.SellerId != sellerId)
                return (ReserveOrderItemsResult.SellerIdMismatch, null);

            if (thisSlot.ValidFromBeforeStartDate.HasValue && DateTime.Now < thisSlot.Start - thisSlot.ValidFromBeforeStartDate)
                return (ReserveOrderItemsResult.OpportunityOfferPairNotBookable, null);

            // Remove existing leases
            // Note a real implementation would likely maintain existing leases instead of removing and recreating them
            await db.DeleteAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == uuid.ToString() && x.SlotId == slotId);
            await RecalculateSlotUses(db, thisSlot);

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
                    OrderId = uuid.ToString(),
                    Status = proposal ? BookingStatus.None : BookingStatus.Confirmed,
                    SlotId = slotId,
                    OpportunityJsonLdId = opportunityJsonLdId,
                    OfferJsonLdId = offerJsonLdId,
                    // Include the price locked into the OrderItem as the opportunity price may change
                    Price = thisSlot.Price.Value,
                    PinCode = Faker.Random.String(6, minChar: '0', maxChar: '9'),
                    ImageUrl = Faker.Image.PlaceholderUrl(width: 25, height: 25),
                    BarCodeText = Faker.Random.String(length: 10, minChar: '0', maxChar: '9'),
                    AttendeeString = attendees.Count > i ? attendees[i] : null,
                    AdditionalDetailsString = additionalDetailsString.Count > i ? additionalDetailsString[i] : null,
                };

                await db.SaveAsync(orderItem);

                bookedOrderItemInfos.Add(new BookedOrderItemInfo
                {
                    OrderItemId = orderItem.Id,
                    PinCode = orderItem.PinCode,
                    ImageUrl = orderItem.ImageUrl,
                    BarCodeText = orderItem.BarCodeText
                });
            }

            await RecalculateSlotUses(db, thisSlot);
            return (ReserveOrderItemsResult.Success, bookedOrderItemInfos);
        }

        public async Task<bool> CancelOrderItems(string clientId, long? sellerId, Guid uuid, List<long> orderItemIds, bool customerCancelled, bool includeCancellationMessage = false)
        {
            using (var db = await Mem.Database.OpenAsync())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {
                var order = customerCancelled
                    ? await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && x.OrderMode == OrderMode.Booking && x.OrderId == uuid.ToString() && !x.Deleted)
                    : await db.SingleAsync<OrderTable>(x => x.OrderId == uuid.ToString() && !x.Deleted);

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
                    .Where(t => t.Item1.Status == BookingStatus.Confirmed || t.Item1.Status == BookingStatus.Attended || t.Item1.Status == BookingStatus.Absent || t.Item1.Status == BookingStatus.CustomerCancelled)
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
                            throw new InvalidOperationException("Customer cancellation not permitted as outside the refund window for the slot");
                        }

                        if (occurrence.Id != 0 &&
                            @class?.LatestCancellationBeforeStartDate != null &&
                            occurrence.Start - @class.LatestCancellationBeforeStartDate < now)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException("Customer cancellation not permitted as outside the refund window for the session");
                        }
                        if (slot.Id != 0 && slot.AllowCustomerCancellationFullRefund == false)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException("Customer cancellation not permitted on this slot");
                        }
                        if (occurrence.Id != 0 &&
                            @class.AllowCustomerCancellationFullRefund == false)
                        {
                            transaction.Rollback();
                            throw new InvalidOperationException("Customer cancellation not permitted on this session");
                        }

                        if (orderItem.Status == BookingStatus.CustomerCancelled)
                        {
                            // If the customer has already cancelled this OrderItem, do nothing to maintain idempotency
                            continue;
                        }

                        orderItem.Status = BookingStatus.CustomerCancelled;
                        updatedOrderItems.Add(orderItem);

                    }
                    else
                    {
                        orderItem.Status = BookingStatus.SellerCancelled;
                        updatedOrderItems.Add(orderItem);

                        if (includeCancellationMessage)
                            orderItem.CancellationMessage = "Order cancelled by seller";
                    }

                    await db.SaveAsync(orderItem);
                }

                // Update the total price and modified date on the Order to update the feed, if something has changed
                // This makes the call idempotent
                if (updatedOrderItems.Count > 0)
                {
                    var totalPrice = (await db.SelectAsync<OrderItemsTable>(x =>
                        x.ClientId == clientId && x.OrderId == order.OrderId &&
                        (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended || x.Status == BookingStatus.Absent))).Sum(x => x.Price);

                    order.TotalOrderPrice = totalPrice;
                    order.VisibleInOrdersFeed = FeedVisibility.Visible;
                    order.OrderModified = DateTimeOffset.Now.UtcTicks;
                    await db.UpdateAsync(order);

                    // Note an actual implementation would need to handle different opportunity types here
                    // Update the number of spaces available as a result of cancellation
                    await RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                    await RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                }

                transaction.Commit();
                return true;
            }
        }

        public async Task<bool> ReplaceOrderOpportunity(Guid uuid)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<OrderItemsTable>()
                              .Join<OrderTable>()
                              .Where<OrderItemsTable>(x => x.OrderId == uuid.ToString())
                              .Where<OrderTable>(x => x.OrderMode != OrderMode.Proposal);
                var orderItemsAndOrder = db.SelectMulti<OrderItemsTable, OrderTable>(query);
                if (!orderItemsAndOrder.Any())
                    return false;
                var order = orderItemsAndOrder.First().Item2;
                var orderItems = orderItemsAndOrder.Select(x => x.Item1).AsList();

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
                    orderItem.OpportunityJsonLdId = new Uri(orderItem.OpportunityJsonLdId.ToString().Replace($"facility-uses/{oldSlot.FacilityUseId}", $"facility-uses/{slot.FacilityUseId}"));
                    orderItem.OpportunityJsonLdId = new Uri(orderItem.OpportunityJsonLdId.ToString().Replace($"facility-use-slots/{oldSlot.Id}", $"facility-use-slots/{slot.Id}"));
                    orderItem.OfferJsonLdId = new Uri(orderItem.OfferJsonLdId.ToString().Replace($"facility-uses/{oldSlot.FacilityUseId}", $"facility-uses/{slot.FacilityUseId}"));
                    orderItem.OfferJsonLdId = new Uri(orderItem.OfferJsonLdId.ToString().Replace($"facility-uses-slots/{oldSlot.Id}", $"facility-uses-slots/{slot.Id}"));

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
                    orderItem.OpportunityJsonLdId = new Uri(orderItem.OpportunityJsonLdId.ToString().Replace($"scheduled-sessions/{oldOccurrence.ClassId}", $"scheduled-sessions/{occurrence.ClassId}"));
                    orderItem.OpportunityJsonLdId = new Uri(orderItem.OpportunityJsonLdId.ToString().Replace($"events/{orderItem.OccurrenceId}", $"events/{occurrence.Id}"));
                    orderItem.OfferJsonLdId = new Uri(orderItem.OfferJsonLdId.ToString().Replace($"session-series/{oldOccurrence.ClassId}", $"session-series/{occurrence.ClassId}"));

                    orderItem.OccurrenceId = occurrence.Id;
                }
                else
                {
                    return false;
                }

                await db.UpdateAsync(orderItem);

                order.TotalOrderPrice = orderItems.Sum(x => x.Price);
                order.VisibleInOrdersFeed = FeedVisibility.Visible;
                order.OrderModified = DateTimeOffset.Now.UtcTicks;
                await db.UpdateAsync(order);

                // Note an actual implementation would need to handle different opportunity types here
                // Update the number of spaces available as a result of cancellation
                await RecalculateSpaces(db, orderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                await RecalculateSlotUses(db, orderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                return true;
            }
        }

        public async Task<bool> AcceptOrderProposal(Guid uuid)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var order = await db.SingleAsync<OrderTable>(x => x.OrderMode == OrderMode.Proposal && x.OrderId == uuid.ToString() && !x.Deleted);
                if (order != null)
                {
                    // This makes the call idempotent
                    if (order.ProposalStatus != ProposalStatus.SellerAccepted)
                    {
                        // Update the status and modified date of the OrderProposal to update the feed
                        order.ProposalStatus = ProposalStatus.SellerAccepted;
                        order.VisibleInOrderProposalsFeed = FeedVisibility.Visible;
                        order.OrderProposalModified = DateTimeOffset.Now.UtcTicks;
                        await db.UpdateAsync(order);
                    }
                    return true;
                }

                return false;
            }
        }
        public async Task<bool> AmendOrderProposal(Guid uuid, Guid version)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var order = await db.SingleAsync<OrderTable>(x => x.OrderMode == OrderMode.Proposal && x.OrderId == uuid.ToString() && !x.Deleted);
                if (order != null)
                {
                    // This makes the call idempotent
                    if (order.ProposalVersionId != version)
                    {
                        // Update the status and modified date of the OrderProposal to update the feed
                        order.ProposalVersionId = version;
                        order.VisibleInOrderProposalsFeed = FeedVisibility.Visible;
                        order.OrderProposalModified = DateTimeOffset.Now.UtcTicks;
                        await db.UpdateAsync(order);
                    }
                    return true;
                }

                return false;
            }
        }

        public async Task<FakeDatabaseBookOrderProposalResult> BookOrderProposal(string clientId, long? sellerId, Guid uuid, Guid? proposalVersionUuid)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                // Note call is idempotent, so it might already be in the booked state
                var order = await db.SingleAsync<OrderTable>(x => x.ClientId == clientId && (x.OrderMode == OrderMode.Proposal || x.OrderMode == OrderMode.Booking) && x.OrderId == uuid.ToString() && !x.Deleted);
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
                    foreach (OrderItemsTable orderItem in await db.SelectAsync<OrderItemsTable>(x => x.ClientId == clientId && x.OrderId == order.OrderId))
                    {
                        if (orderItem.Status != BookingStatus.Confirmed)
                        {
                            updatedOrderItems.Add(orderItem);
                            orderItem.Status = BookingStatus.Confirmed;
                            await db.SaveAsync(orderItem);
                        }
                    }
                    // Update the status and modified date of the OrderProposal to update the feed, if something has changed
                    // This makes the call idempotent
                    if (updatedOrderItems.Count > 0 || order.OrderMode != OrderMode.Booking)
                    {
                        order.OrderMode = OrderMode.Booking;
                        order.VisibleInOrderProposalsFeed = FeedVisibility.Archived;
                        order.OrderProposalModified = DateTimeOffset.Now.UtcTicks;
                        await db.UpdateAsync(order);
                        // Note an actual implementation would need to handle different opportunity types here
                        // Update the number of spaces available as a result of cancellation
                        await RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                        await RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                    }
                    return FakeDatabaseBookOrderProposalResult.OrderSuccessfullyBooked;
                }

                return FakeDatabaseBookOrderProposalResult.OrderWasNotFound;
            }
        }

        public async Task<long> GetNumberOfOtherLeaseForOccurrence(Guid uuid, long? occurrenceId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return db.Count<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected &&
                                                 x.OccurrenceId == occurrenceId &&
                                                 x.OrderId != uuid.ToString());
            }
        }

        public async Task<long> GetNumberOfOtherLeasesForSlot(Guid uuid, long? slotId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return db.Count<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected &&
                                                 x.SlotId == slotId &&
                                                 x.OrderId != uuid.ToString());
            }
        }

        public async Task<bool> RejectOrderProposal(string clientId, long? sellerId, Guid uuid, bool customerRejected)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var order = await db.SingleAsync<OrderTable>(x => (clientId == null || x.ClientId == clientId) && x.OrderMode == OrderMode.Proposal && x.OrderId == uuid.ToString() && !x.Deleted);
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
                        order.VisibleInOrderProposalsFeed = FeedVisibility.Visible;
                        order.OrderProposalModified = DateTimeOffset.Now.UtcTicks;
                        await db.UpdateAsync(order);
                        // Note an actual implementation would need to handle different opportunity types here
                        // Update the number of spaces available as a result of cancellation
                        List<OrderItemsTable> updatedOrderItems = (await db.SelectAsync<OrderItemsTable>(x => (clientId == null || x.ClientId == clientId) && x.OrderId == order.OrderId)).AsList();
                        await RecalculateSpaces(db, updatedOrderItems.Where(x => x.OccurrenceId.HasValue).Select(x => x.OccurrenceId.Value).Distinct());
                        await RecalculateSlotUses(db, updatedOrderItems.Where(x => x.SlotId.HasValue).Select(x => x.SlotId.Value).Distinct());
                    }
                    return true;
                }

                return false;
            }
        }

        public static async Task RecalculateSlotUses(IDbConnection db, SlotTable slot)
        {
            if (slot == null)
                return;

            // Update number of leased spaces remaining for the opportunity
            var leasedUses = (await db.LoadSelectAsync<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking && x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected && x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected && x.SlotId == slot.Id)).Count;
            slot.LeasedUses = leasedUses;

            // Update number of actual spaces remaining for the opportunity
            var totalUsesTaken = (await db.LoadSelectAsync<OrderItemsTable>(x => x.OrderTable.OrderMode == OrderMode.Booking && x.OccurrenceId == slot.Id && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended || x.Status == BookingStatus.Absent))).Count;
            slot.RemainingUses = slot.MaximumUses - totalUsesTaken;

            // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition)
            // TODO: Document this!
            slot.Modified = DateTimeOffset.Now.UtcTicks;
            await db.UpdateAsync(slot);
        }

        public static async Task RecalculateSlotUses(IDbConnection db, IEnumerable<long> slotIds)
        {
            foreach (var slotId in slotIds)
            {
                var thisSlot = await db.SingleAsync<SlotTable>(x => x.Id == slotId && !x.Deleted);
                await RecalculateSlotUses(db, thisSlot);
            }
        }

        public static async Task RecalculateSpaces(IDbConnection db, OccurrenceTable occurrence)
        {
            if (occurrence == null)
                return;

            // Update number of leased spaces remaining for the opportunity
            var leasedSpaces = (await db.LoadSelectAsync<OrderItemsTable>(x => x.OrderTable.OrderMode != OrderMode.Booking && x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected && x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected && x.OccurrenceId == occurrence.Id)).Count;
            occurrence.LeasedSpaces = leasedSpaces;

            // Update number of actual spaces remaining for the opportunity
            var totalSpacesTaken = (await db.LoadSelectAsync<OrderItemsTable>(x => x.OrderTable.OrderMode == OrderMode.Booking && x.OccurrenceId == occurrence.Id && (x.Status == BookingStatus.Confirmed || x.Status == BookingStatus.Attended || x.Status == BookingStatus.Absent))).Count();
            occurrence.RemainingSpaces = occurrence.TotalSpaces - totalSpacesTaken;

            // Push the change into the future to avoid it getting lost in the feed (see race condition transaction challenges https://developer.openactive.io/publishing-data/data-feeds/implementing-rpde-feeds#preventing-the-race-condition) // TODO: Document this!
            occurrence.Modified = DateTimeOffset.Now.UtcTicks;
            await db.UpdateAsync(occurrence);
        }

        public static async Task RecalculateSpaces(IDbConnection db, IEnumerable<long> occurrenceIds)
        {
            foreach (var occurrenceId in occurrenceIds)
            {
                var thisOccurrence = await db.SingleAsync<OccurrenceTable>(x => x.Id == occurrenceId && !x.Deleted);
                await RecalculateSpaces(db, thisOccurrence);
            }
        }

        public static async Task<FakeDatabase> GetPrepopulatedFakeDatabase(bool facilityUseHasSlots)
        {
            var database = new FakeDatabase(facilityUseHasSlots);
            using (var db = await database.Mem.Database.OpenAsync())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {

                await CreateSellers(db);
                await CreateSellerUsers(db);
                await CreateFakeClasses(db);
                await CreateFakeFacilitiesAndSlots(db, facilityUseHasSlots);
                await CreateOrders(db); // Add these in to generate your own orders and grants, otherwise generate them using the test suite
                await CreateGrants(db);
                await BookingPartnerTable.Create(db);
                transaction.Commit();

            }

            return database;
        }

        private static async Task CreateFakeFacilitiesAndSlots(IDbConnection db, bool facilityUseHasSlots)
        {
            var opportunitySeeds = GenerateOpportunitySeedDistribution(OpportunityCount);

            var slotId = 0;
            List<(FacilityUseTable facility, List<SlotTable> slots)> facilitiesAndSlots = opportunitySeeds.Select((seed) =>
            {
                var facilityUseName = $"{Faker.Commerce.ProductMaterial()} {Faker.PickRandomParam("Sports Hall", "Swimming Pool Hall", "Running Hall", "Jumping Hall")}";
                var facility = new FacilityUseTable
                {
                    Id = seed.Id,
                    Deleted = false,
                    Name = facilityUseName,
                    SellerId = Faker.Random.Bool(0.8f) ? Faker.Random.Long(1, 2) : Faker.Random.Long(3, 5), // distribution: 80% 1-2, 20% 3-5 
                    PlaceId = Faker.PickRandom(new[] { 1, 2, 3 })
                };

                // If facilityUseHasSlots=false, generate 10 IFUs with each with a randomly generated number of Slots each with MaximumUses=1
                List<SlotTable> slots;
                if (!facilityUseHasSlots)
                {
                    // Create random Individual Facility Uses
                    var individualFacilityUses = Enumerable.Range(0, 10).Select(i => new IndividualFacilityUse
                    {
                        Id = i,
                        Name = $"Court {i} at {facility.Name}",
                        SportActivityLocationName = $"Court {i}"
                    }).AsList();
                    facility.IndividualFacilityUses = individualFacilityUses;

                    slots = individualFacilityUses.Select(ifu => new
                    {
                        StartDate = seed.RandomStartDate(),
                        TotalUses = 1,
                        Price = decimal.Parse(Faker.Random.Bool() ? "0.00" : Faker.Commerce.Price((decimal)0.5, 20)),
                        IndividualFacilityUseId = ifu.Id,
                    })
                    .Select(slot => GenerateSlot(seed, ref slotId, slot.StartDate, slot.TotalUses, slot.Price))
                    .AsList();
                }
                else
                {
                    slots = Enumerable.Range(0, 10)
                        .Select(_ => new
                        {
                            StartDate = seed.RandomStartDate(),
                            TotalUses = Faker.Random.Int(0, 8),
                            Price = decimal.Parse(Faker.Random.Bool() ? "0.00" : Faker.Commerce.Price((decimal)0.5, 20)),
                        })
                        .Select(slot => GenerateSlot(seed, ref slotId, slot.StartDate, slot.TotalUses, slot.Price))
                        .AsList();
                }

                return (facility, slots);
            })
            .AsList();

            var facilities = facilitiesAndSlots.Select(facilityAndSlots => facilityAndSlots.facility);
            var slotTableSlots = facilitiesAndSlots.SelectMany(facilityAndSlots => facilityAndSlots.slots);
            await db.InsertAllAsync(facilities);
            await db.InsertAllAsync(slotTableSlots);
        }
        private static SlotTable GenerateSlot(OpportunitySeed seed, ref int slotId, DateTime startDate, int totalUses, decimal price)
        {
            var requiresAdditionalDetails = Faker.Random.Bool(ProportionWithRequiresAdditionalDetails);
            return new SlotTable
            {
                FacilityUseId = seed.Id,
                Id = slotId++,
                Deleted = false,
                Start = startDate,
                End = startDate + TimeSpan.FromMinutes(Faker.Random.Int(30, 360)),
                MaximumUses = totalUses,
                RemainingUses = totalUses,
                Price = price,
                Prepayment = price == 0
                    ? Faker.Random.Bool() ? RequiredStatusType.Unavailable : (RequiredStatusType?)null
                    : Faker.Random.Bool() ? Faker.Random.Enum<RequiredStatusType>() : (RequiredStatusType?)null,
                RequiresAttendeeValidation = Faker.Random.Bool(ProportionWithRequiresAttendeeValidation),
                RequiresAdditionalDetails = requiresAdditionalDetails,
                RequiredAdditionalDetails = requiresAdditionalDetails ? PickRandomAdditionalDetails() : null,
                RequiresApproval = seed.RequiresApproval,
                AllowsProposalAmendment = seed.RequiresApproval && Faker.Random.Bool(),
                ValidFromBeforeStartDate = seed.RandomValidFromBeforeStartDate(),
                LatestCancellationBeforeStartDate = RandomLatestCancellationBeforeStartDate(),
                AllowCustomerCancellationFullRefund = Faker.Random.Bool()
            };
        }

        public static async Task CreateFakeClasses(IDbConnection db)
        {
            var opportunitySeeds = GenerateOpportunitySeedDistribution(OpportunityCount);

            var classes = opportunitySeeds
                .Select(seed => new
                {
                    seed.Id,
                    Price = decimal.Parse(Faker.Random.Bool() ? "0.00" : Faker.Commerce.Price((decimal)0.5, 20)),
                    ValidFromBeforeStartDate = seed.RandomValidFromBeforeStartDate(),
                    seed.RequiresApproval
                })
                .Select(@class =>
                {
                    var requiresAdditionalDetails = Faker.Random.Bool(ProportionWithRequiresAdditionalDetails);
                    return new ClassTable
                    {
                        Id = @class.Id,
                        Deleted = false,
                        Title = $"{Faker.Commerce.ProductMaterial()} {Faker.PickRandomParam("Yoga", "Zumba", "Walking", "Cycling", "Running", "Jumping")}",
                        Price = @class.Price,
                        Prepayment = @class.Price == 0
                            ? Faker.Random.Bool() ? RequiredStatusType.Unavailable : (RequiredStatusType?)null
                            : Faker.Random.Bool() ? Faker.Random.Enum<RequiredStatusType>() : (RequiredStatusType?)null,
                        RequiresAttendeeValidation = Faker.Random.Bool(ProportionWithRequiresAttendeeValidation),
                        RequiresAdditionalDetails = requiresAdditionalDetails,
                        RequiredAdditionalDetails = requiresAdditionalDetails ? PickRandomAdditionalDetails() : null,
                        RequiresApproval = @class.RequiresApproval,
                        AllowsProposalAmendment = @class.RequiresApproval && Faker.Random.Bool(),
                        LatestCancellationBeforeStartDate = RandomLatestCancellationBeforeStartDate(),
                        SellerId = Faker.Random.Bool(0.8f) ? Faker.Random.Long(1, 2) : Faker.Random.Long(3, 5), // distribution: 80% 1-2, 20% 3-5
                        ValidFromBeforeStartDate = @class.ValidFromBeforeStartDate,
                        AttendanceMode = Faker.PickRandom<AttendanceMode>(),
                        AllowCustomerCancellationFullRefund = Faker.Random.Bool(),
                        PlaceId = Faker.PickRandom(new[] { 1, 2, 3 })
                    };
                })
                .AsList();

            var occurrenceId = 0;
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

            await db.InsertAllAsync(classes);
            await db.InsertAllAsync(occurrences);
        }

        public static async Task CreateSellers(IDbConnection db)
        {
            var sellers = new List<SellerTable>
            {
                new SellerTable { Id = 1, Name = "Acme Fitness Ltd", IsIndividual = false, LogoUrl = "https://placekitten.com/640/360", Url = "https://www.example.com", IsTaxGross = true },
                new SellerTable { Id = 2, Name = "Road Runner Bookcamp Ltd", IsIndividual = false, LogoUrl = "https://placekitten.com/640/360", Url = "https://www.example.com", IsTaxGross = false },
                new SellerTable { Id = 3, Name = "Lorem Fitsum Ltd", IsIndividual = false, LogoUrl = "https://placekitten.com/640/360", Url = "https://www.example.com", IsTaxGross = true },
                new SellerTable { Id = 4, Name = "Coyote Classes Ltd", IsIndividual = false, LogoUrl = "https://placekitten.com/640/360", Url = "https://www.example.com", IsTaxGross = false },
                new SellerTable { Id = 5, Name = "Jane Smith", IsIndividual = true, LogoUrl = "https://placekitten.com/640/360", Url = "https://www.example.com", IsTaxGross = true }
            };

            await db.InsertAllAsync(sellers);
        }

        public static async Task CreateOrders(IDbConnection db)
        {
            var orders = new List<OrderTable>
            {
                new OrderTable
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Deleted = false,
                    OrderCreated = DateTimeOffset.Now,
                    OrderModified = DateTimeOffset.Now.Ticks,
                    OrderProposalModified = DateTimeOffset.Now.Ticks,
                    ClientId = "clientid_XXX",
                    SellerId = 1,
                    CustomerType = CustomerType.Person,
                    BrokerRole = BrokerRole.AgentBroker,
                    BrokerName = "Adult Fitness Challenge",
                    BrokerUrl = new Uri("https://myfitnessapp.example.com/"),
                    CustomerEmail = "Ardith72@hotmail.com",
                    CustomerIdentifier = Guid.NewGuid().ToString(),
                    CustomerGivenName = "Hills",
                    CustomerFamilyName = "Modesta",
                    CustomerTelephone = "731.403.0727",
                    PaymentIdentifier = "dyulZE-Kt",
                    PaymentName = "AcmeBroker Points",
                    PaymentProviderId = "STRIPE",
                    PaymentAccountId = "SN1593",
                    TotalOrderPrice = 14.99M,
                    OrderMode = OrderMode.Booking,
                    LeaseExpires = DateTime.Now.AddDays(10),
                    VisibleInOrdersFeed = FeedVisibility.None
                },
                new OrderTable
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Deleted = false,
                    OrderCreated = DateTimeOffset.Now,
                    OrderModified = DateTimeOffset.Now.Ticks,
                    OrderProposalModified = DateTimeOffset.Now.Ticks,
                    ClientId = "clientid_YYY",
                    SellerId = 1,
                    CustomerType = CustomerType.Person,
                    BrokerRole = BrokerRole.AgentBroker,
                    BrokerName = "Healthy Steps App",
                    BrokerUrl = new Uri("https://myfitnessapp.example.com/"),
                    CustomerEmail = "Zelma.Pacocha79@gmail.com",
                    CustomerIdentifier = Guid.NewGuid().ToString(),
                    CustomerGivenName = "Boyer",
                    CustomerFamilyName = "Santos",
                    CustomerTelephone = "1-346-608-5991 x53561",
                    PaymentIdentifier = "JU1ktRR7U",
                    PaymentName = "AcmeBroker Points",
                    PaymentProviderId = "STRIPE",
                    PaymentAccountId = "SN1593",
                    TotalOrderPrice = 14.99M,
                    OrderMode = OrderMode.Booking,
                    LeaseExpires = DateTime.Now.AddDays(10),
                    VisibleInOrdersFeed = FeedVisibility.None
                },
                new OrderTable
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Deleted = false,
                    OrderCreated = DateTimeOffset.Now,
                    OrderModified = DateTimeOffset.Now.Ticks,
                    OrderProposalModified = DateTimeOffset.Now.Ticks,
                    ClientId = "clientid_YYY",
                    SellerId = 1,
                    CustomerType = CustomerType.Person,
                    BrokerRole = BrokerRole.AgentBroker,
                    BrokerName = "Healthy Steps Website",
                    BrokerUrl = new Uri("https://myfitnessapp.example.com/"),
                    CustomerEmail = "Regan_Moen4@gmail.com",
                    CustomerIdentifier = Guid.NewGuid().ToString(),
                    CustomerGivenName = "Kohler",
                    CustomerFamilyName = "Toby",
                    CustomerTelephone = "1-585-849-0456",
                    PaymentIdentifier = "Lr4GW6MNQ",
                    PaymentName = "AcmeBroker Points",
                    PaymentProviderId = "STRIPE",
                    PaymentAccountId = "SN1593",
                    TotalOrderPrice = 59.96M,
                    OrderMode = OrderMode.Booking,
                    LeaseExpires = DateTime.Now.AddDays(10),
                    VisibleInOrdersFeed = FeedVisibility.None
                }
            };

            await db.InsertAllAsync(orders);
        }

        public static async Task CreateGrants(IDbConnection db)
        {
            var grants = new List<GrantTable>
            {
                new GrantTable { ClientId = "clientid_XXX", SubjectId = "100", Type = "user_consent" },
                new GrantTable { ClientId = "clientid_YYY", SubjectId = "100", Type = "user_consent" },
                new GrantTable { ClientId = "clientid_ZZZ", SubjectId = "100", Type = "user_consent" },
            };

            await db.InsertAllAsync(grants);
        }

        public static readonly SellerUserTable[] DefaultSellerUsers =
        {
            new SellerUserTable {Id = 100, Username = "test1", PasswordRaw = "test1", SellerId = 1},
            new SellerUserTable {Id = 101, Username = "test1b", PasswordRaw = "test1b", SellerId = 1},
            new SellerUserTable {Id = 102, Username = "test2", PasswordRaw = "test2", SellerId = 2},
            new SellerUserTable {Id = 103, Username = "test3", PasswordRaw = "test3", SellerId = 3},
            new SellerUserTable {Id = 104, Username = "test4", PasswordRaw = "test4", SellerId = 4},
            new SellerUserTable {Id = 105, Username = "test5", PasswordRaw = "test5", SellerId = 5},
        };

        public static async Task CreateSellerUsers(IDbConnection db)
        {
            await db.InsertAllAsync(DefaultSellerUsers);
        }

        public async Task<bool> ValidateSellerUserCredentials(string username, string password)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var matchingUser = await db.SingleAsync<SellerUserTable>(x => x.Username == username && x.PasswordHash == password.Sha256());
                return matchingUser != null;
            }
        }

        public async Task<SellerUserTable> GetSellerUser(string username)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return await db.SingleAsync<SellerUserTable>(x => x.Username == username);
            }
        }

        public async Task<SellerUserTable> GetSellerUserById(long sellerUserId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return await db.LoadSingleByIdAsync<SellerUserTable>(sellerUserId);
            }
        }

        public async Task<GrantTable> GetGrant(string key)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return await db.SingleAsync<GrantTable>(x => x.Key == key);
            }
        }
        public async Task<List<GrantTable>> GetAllGrants(string subjectId, string sessionId, string clientId, string type)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<GrantTable>();
                if (!String.IsNullOrWhiteSpace(clientId))
                {
                    query = query.Where(x => x.ClientId == clientId);
                }
                if (!String.IsNullOrWhiteSpace(sessionId))
                {
                    query = query.Where(x => x.SessionId == sessionId);
                }
                if (!String.IsNullOrWhiteSpace(subjectId))
                {
                    query = query.Where(x => x.SubjectId == subjectId);
                }
                if (!String.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(x => x.Type == type);
                }

                return (await db.SelectAsync(query)).AsList();
            }
        }

        public async Task<bool> AddGrant(string key, string type, string subjectId, string sessionId, string clientId, DateTime creationTime, DateTime? consumedTime, DateTime? expiration, string data)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var grant = new GrantTable
                {
                    Key = key,
                    Type = type,
                    SubjectId = subjectId,
                    SessionId = sessionId,
                    ClientId = clientId,
                    CreationTime = creationTime,
                    ConsumedTime = consumedTime,
                    Expiration = expiration,
                    Data = data
                };
                return await db.SaveAsync(grant);
            }
        }

        public async Task RemoveGrant(string key)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.DeleteAsync<GrantTable>(x => x.Key == key);
            }
        }

        public async Task RemoveAllGrants(string subjectId, string sessionId, string clientId, string type)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<GrantTable>();
                if (!String.IsNullOrWhiteSpace(clientId))
                {
                    query = query.Where(x => x.ClientId == clientId);
                }
                if (!String.IsNullOrWhiteSpace(sessionId))
                {
                    query = query.Where(x => x.SessionId == sessionId);
                }
                if (!String.IsNullOrWhiteSpace(subjectId))
                {
                    query = query.Where(x => x.SubjectId == subjectId);
                }
                if (!String.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(x => x.Type == type);
                }

                await db.DeleteAsync(query);
            }
        }

        public async Task<(int, int)> AddClass(
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
            bool requiresAdditionalDetails = false,
            bool isOnlineOrMixedAttendanceMode = false,
            bool allowProposalAmendment = false,
            bool inPast = false)

        {
            var startTime = DateTime.Now.AddDays(inPast ? -1 : 1);
            var endTime = DateTime.Now.AddDays(inPast ? -1 : 1).AddHours(1);

            using (var db = await Mem.Database.OpenAsync())
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
                    RequiresAdditionalDetails = requiresAdditionalDetails,
                    RequiredAdditionalDetails = requiresAdditionalDetails ? PickRandomAdditionalDetails() : null,
                    AllowsProposalAmendment = allowProposalAmendment,
                    PlaceId = Faker.PickRandom(new[] { 1, 2, 3 }),
                    AttendanceMode = isOnlineOrMixedAttendanceMode ? Faker.PickRandom(new[] { AttendanceMode.Mixed, AttendanceMode.Online }) : AttendanceMode.Offline,
                    AllowCustomerCancellationFullRefund = allowCustomerCancellationFullRefund,
                    Modified = DateTimeOffset.Now.UtcTicks
                };
                await db.SaveAsync(@class);

                var occurrence = new OccurrenceTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    ClassId = @class.Id,
                    Start = startTime,
                    End = endTime,
                    TotalSpaces = totalSpaces,
                    RemainingSpaces = totalSpaces,
                    Modified = DateTimeOffset.Now.UtcTicks
                };
                await db.SaveAsync(occurrence);

                transaction.Commit();

                return ((int)@class.Id, (int)occurrence.Id);
            }
        }

        public async Task<(int, int?, int)> AddFacility(
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
            bool requiresAdditionalDetails = false,
            decimal locationLng = 0.1m,
            bool allowProposalAmendment = false,
            bool inPast = false
            )
        {
            var startTime = DateTime.Now.AddDays(inPast ? -1 : 1);
            var endTime = DateTime.Now.AddDays(inPast ? -1 : 1).AddHours(1);

            using (var db = await Mem.Database.OpenAsync())
            using (var transaction = db.OpenTransaction(IsolationLevel.Serializable))
            {

                var facility = new FacilityUseTable
                {
                    TestDatasetIdentifier = testDatasetIdentifier,
                    Deleted = false,
                    Name = title,
                    SellerId = sellerId ?? 1,
                    PlaceId = Faker.PickRandom(new[] { 1, 2, 3 }),
                    Modified = DateTimeOffset.Now.UtcTicks
                };
                if (!_facilityUseHasSlots)
                {
                    facility.IndividualFacilityUses = new List<IndividualFacilityUse> {
                        new IndividualFacilityUse {
                             Id = 1,
                            Name = $"Court {1} on {title}",
                            SportActivityLocationName = $"Court {1}"
                        }
                    };
                }
                await db.SaveAsync(facility);

                int? individualFacilityUseId = null;
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
                    RequiresAdditionalDetails = requiresAdditionalDetails,
                    RequiredAdditionalDetails = requiresAdditionalDetails ? PickRandomAdditionalDetails() : null,
                    AllowsProposalAmendment = allowProposalAmendment,
                    AllowCustomerCancellationFullRefund = allowCustomerCancellationFullRefund,
                    Modified = DateTimeOffset.Now.UtcTicks
                };
                if (!_facilityUseHasSlots)
                {
                    individualFacilityUseId = 1;
                    slot.IndividualFacilityUseId = individualFacilityUseId;
                }
                await db.SaveAsync(slot);

                transaction.Commit();


                return ((int)facility.Id, individualFacilityUseId, (int)slot.Id);
            }
        }

        public async Task DeleteTestClassesFromDataset(string testDatasetIdentifier)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.UpdateOnlyAsync(() => new ClassTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);

                await db.UpdateOnlyAsync(() => new OccurrenceTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);
            }
        }

        public async Task DeleteTestFacilitiesFromDataset(string testDatasetIdentifier)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.UpdateOnlyAsync(() => new FacilityUseTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);

                await db.UpdateOnlyAsync(() => new SlotTable { Modified = DateTimeOffset.Now.UtcTicks, Deleted = true },
                    where: x => x.TestDatasetIdentifier == testDatasetIdentifier && !x.Deleted);
            }
        }

        public async Task<List<BookingPartnerTable>> BookingPartnerGet()
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return await db.SelectAsync<BookingPartnerTable>();
            }
        }

        public async Task<List<BookingPartnerTable>> BookingPartnerGetBySellerUserId(long sellerUserId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var query = db.From<BookingPartnerTable>()
                              .Join<BookingPartnerTable, GrantTable>((b, g) => b.ClientId == g.ClientId && g.Type == "user_consent")
                              .Join<GrantTable, SellerUserTable>((g, s) => g.SubjectId == s.Id.ToString())
                              .Where<SellerUserTable>(s => s.Id == sellerUserId);
                return await db.SelectAsync(query);
            }
        }

        public async Task<BookingPartnerTable> BookingPartnerGetByInitialAccessToken(string registrationKey)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.InitialAccessToken == registrationKey);
                return bookingPartner?.InitialAccessTokenKeyValidUntil > DateTime.Now ? bookingPartner : null;
            }
        }

        public async Task<BookingPartnerTable> BookingPartnerGetByClientId(string clientId)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                return await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
            }
        }

        public async Task BookingPartnerSave(BookingPartnerTable bookingPartnerTable)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.SaveAsync(bookingPartnerTable);
            }
        }

        public async Task BookingPartnerResetCredentials(string clientId, string key)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.Registered = false;
                bookingPartner.InitialAccessToken = key;
                bookingPartner.InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(2);
                bookingPartner.ClientSecret = null;
                await db.SaveAsync(bookingPartner);
            }
        }

        public async Task BookingPartnerSetKey(string clientId, string key)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.InitialAccessToken = key;
                bookingPartner.InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(2);
                await db.SaveAsync(bookingPartner);
            }
        }

        public async Task BookingPartnerUpdateScope(string clientId, string scope, bool bookingsSuspended)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                var bookingPartner = await db.SingleAsync<BookingPartnerTable>(x => x.ClientId == clientId);
                bookingPartner.Scope = scope;
                bookingPartner.BookingsSuspended = true;
                await db.SaveAsync(bookingPartner);
            }
        }

        public async Task BookingPartnerAdd(BookingPartnerTable newBookingPartner)
        {
            using (var db = await Mem.Database.OpenAsync())
            {
                await db.SaveAsync(newBookingPartner);
            }
        }

        private static readonly (Bounds, Bounds?, bool)[] BucketDefinitions =
        {
            // Approval not required
            // in next 0-10 days, no validFromBeforeStartDate
            (new Bounds(0, 10), null, false),
            (new Bounds(0, 10), null, false),
            // in next 0-10 days, validFromBeforeStartDate between 10-15 days (all bookable)
            (new Bounds(0, 10), new Bounds(10, 15), false),
            (new Bounds(0, 10), new Bounds(10, 15), false),
            // in next -2-+6 days, validFromBeforeStartDate 0-4 days (over half likely bookable, some likely bookable but in the past)
            (new Bounds(-2, 6), new Bounds(0, 4), false),
            // in next 5-10 days, validFromBeforeStartDate between 0-4 days (none bookable)
            (new Bounds(5, 10), new Bounds(0, 4), false),

            // Approval  required
            // in next 0-10 days, no validFromBeforeStartDate
            (new Bounds(0, 10), null, true),
            (new Bounds(0, 10), null, true),
            // in next 0-10 days, validFromBeforeStartDate between 10-15 days (all bookable)
            (new Bounds(0, 10), new Bounds(10, 15), true),
            (new Bounds(0, 10), new Bounds(10, 15), true),
            // in next -2-+6 days, validFromBeforeStartDate 0-4 days (over half likely bookable, some likely bookable but in the past)
            (new Bounds(-2, 6), new Bounds(0, 4), true),
            // in next 5-10 days, validFromBeforeStartDate between 0-4 days (none bookable)
            (new Bounds(5, 10), new Bounds(0, 4), true),
        };

        private static OpportunitySeed GenerateRandomOpportunityData(Faker faker, int index, (Bounds startDateRange, Bounds? validFromBeforeStartDateRange, bool requiresApproval) input)
        {
            return new OpportunitySeed
            {
                SeedFaker = faker,
                Id = index + 1,
                StartDateBounds = BoundsDaysToMinutes(input.startDateRange).Value,
                ValidFromBeforeStartDateBounds = !input.validFromBeforeStartDateRange.HasValue ? (Bounds?)null : BoundsDaysToMinutes(input.validFromBeforeStartDateRange).Value,
                RequiresApproval = input.requiresApproval,
            };
        }

        private static Bounds? BoundsDaysToMinutes(Bounds? bounds)
        {
            const int minutesInDay = 60 * 24;
            return !bounds.HasValue ? (Bounds?)null : new Bounds(bounds.Value.Lower * minutesInDay, bounds.Value.Upper * minutesInDay);
        }

        /// <summary>
        /// Used to generate random data.
        /// </summary>
        private static List<OpportunitySeed> GenerateOpportunitySeedDistribution(int count)
        {
            return Faker.GenerateIntegerDistribution(count, BucketDefinitions, GenerateRandomOpportunityData).AsList();
        }

        private struct OpportunitySeed
        {
            public Faker SeedFaker { get; set; }
            public int Id { get; set; }
            public Bounds StartDateBounds { get; set; }
            public Bounds? ValidFromBeforeStartDateBounds { get; set; }

            public DateTime RandomStartDate() => DateTime.Now.AddMinutes(SeedFaker.Random.Int(StartDateBounds));

            public TimeSpan? RandomValidFromBeforeStartDate() => ValidFromBeforeStartDateBounds.HasValue
                ? TimeSpan.FromMinutes(SeedFaker.Random.Int(ValidFromBeforeStartDateBounds.Value))
                : (TimeSpan?)null;
            public bool RequiresApproval { get; set; }
        }

        private static TimeSpan? RandomLatestCancellationBeforeStartDate()
        {
            if (Faker.Random.Bool(1f / 3))
                return null;

            return Faker.Random.Bool() ? TimeSpan.FromDays(1) : TimeSpan.FromHours(40);
        }

        private static List<AdditionalDetailTypes> PickRandomAdditionalDetails()
        {
            return new HashSet<AdditionalDetailTypes> { Faker.PickRandom<AdditionalDetailTypes>(), Faker.PickRandom<AdditionalDetailTypes>() }.AsList();
        }
    }
}
