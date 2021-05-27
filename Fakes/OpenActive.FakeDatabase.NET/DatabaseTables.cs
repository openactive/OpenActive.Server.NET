using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{
    public enum BrokerRole { AgentBroker, ResellerBroker, NoBroker }

    public enum BookingStatus { None, CustomerCancelled, SellerCancelled, Confirmed, Attended, Proposed }

    public enum ProposalStatus { AwaitingSellerConfirmation, SellerAccepted, SellerRejected, CustomerRejected }

    public enum FeedVisibility { None, Visible, Archived }

    public enum OrderMode { Lease, Proposal, Booking }

    public enum RequiredStatusType { Required, Optional, Unavailable }

    public enum AttendanceMode { Offline, Online, Mixed }

    public enum AdditionalDetailTypes { Age, PhotoConsent, Experience, Gender }

    [CompositeIndex(nameof(Modified), nameof(Id))]
    public abstract class Table
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
        public bool Deleted { get; set; }
        public long Modified { get; set; } = new DateTimeOffset(DateTime.Today).UtcTicks;
    }

    public class ClassTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        public string Title { get; set; }
        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
        public decimal? Price { get; set; }
        public RequiredStatusType? Prepayment { get; set; }
        public bool RequiresAttendeeValidation { get; set; }
        public bool RequiresAdditionalDetails { get; set; }
        public List<AdditionalDetailTypes> RequiredAdditionalDetails { get; set; }
        public bool AllowCustomerCancellationFullRefund { get; set; }
        public bool RequiresApproval { get; set; }
        public TimeSpan? ValidFromBeforeStartDate { get; set; }
        public TimeSpan? LatestCancellationBeforeStartDate { get; set; }
        public decimal LocationLat { get; set; }
        public decimal LocationLng { get; set; }
        public AttendanceMode AttendanceMode { get; set; }
        public bool AllowsProposalAmendment { get; set; }
        public DayOfWeek PartialScheduleDay { get; set; }
        public DateTime PartialScheduleTime { get; set; }
        public TimeSpan PartialScheduleDuration { get; set; }

        // Due to ORMLite free tier limit, we can only have 10 tables
        // So instead of having a separate EventTable, Events are just Classes with time-specific info
        public bool IsEvent { get; set; } = false;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long TotalSpaces { get; set; }
        public long LeasedSpaces { get; set; }
        public long RemainingSpaces { get; set; }
    }

    public class OccurrenceTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        [Reference]
        public ClassTable ClassTable { get; set; }
        [ForeignKey(typeof(ClassTable), OnDelete = "CASCADE")]
        public long ClassId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long TotalSpaces { get; set; }
        public long LeasedSpaces { get; set; }
        public long RemainingSpaces { get; set; }
    }

    public class OrderItemsTable : Table
    {
        public string ClientId { get; internal set; }
        public Uri OpportunityJsonLdId { get; set; }
        public Uri OfferJsonLdId { get; set; }
        [Reference]
        public OrderTable OrderTable { get; set; }
        [ForeignKey(typeof(OrderTable), OnDelete = "CASCADE")]
        public string OrderId { get; set; }
        [Reference]
        public OccurrenceTable OccurrenceTable { get; set; }
        [ForeignKey(typeof(OccurrenceTable), OnDelete = "CASCADE")]
        public long? OccurrenceId { get; set; }
        [Reference]
        public SlotTable SlotTable { get; set; }
        [ForeignKey(typeof(SlotTable), OnDelete = "CASCADE")]
        public long? SlotId { get; set; }
        [Reference]
        public ClassTable EventTable { get; set; }
        [ForeignKey(typeof(ClassTable), OnDelete = "CASCADE")]
        public long? EventId { get; set; }
        public BookingStatus Status { get; set; }
        public string CancellationMessage { get; set; }
        public decimal Price { get; set; }
        public string PinCode { get; set; }
        public string ImageUrl { get; set; }
        public string BarCodeText { get; set; }
        public string CustomerNotice { get; set; }
        public Uri MeetingUrl { get; set; }
        public string MeetingId { get; set; }
        public string MeetingPassword { get; set; }
    }

    [CompositeIndex(nameof(OrderModified), nameof(OrderId))]
    [CompositeIndex(nameof(OrderProposalModified), nameof(OrderId))]
    public class OrderTable
    {
        [PrimaryKey]
        public string OrderId { get; set; }

        public bool Deleted { get; set; }
        public long OrderModified { get; set; } = DateTimeOffset.Now.UtcTicks;
        public long OrderProposalModified { get; set; } = DateTimeOffset.Now.UtcTicks;
        public string ClientId { get; set; }

        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
        public bool? CustomerIsOrganization { get; set; }
        public CustomerType CustomerType { get; set; }
        public BrokerRole BrokerRole { get; set; }
        public string BrokerName { get; set; }
        public Uri BrokerUrl { get; set; }
        public string BrokerTelephone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerIdentifier { get; set; }
        public string CustomerGivenName { get; set; }
        public string CustomerFamilyName { get; set; }
        public string CustomerTelephone { get; set; }
        public string CustomerOrganizationName { get; set; }
        public string PaymentIdentifier { get; set; }
        public string PaymentName { get; set; }
        public string PaymentProviderId { get; set; }
        public string PaymentAccountId { get; set; }
        public decimal TotalOrderPrice { get; set; }
        public OrderMode OrderMode { get; set; }
        public DateTime LeaseExpires { get; set; }
        public FeedVisibility VisibleInOrdersFeed { get; set; }
        public FeedVisibility VisibleInOrderProposalsFeed { get; set; }
        public ProposalStatus? ProposalStatus { get; set; }
        public Guid? ProposalVersionId { get; set; }
    }

    public class SellerTable
    {
        [PrimaryKey]
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsIndividual { get; set; }
        public string Url { get; set; }
        public bool IsTaxGross { get; set; }
        public string LogoUrl { get; set; }
    }

    public class SellerUserTable
    {
        [PrimaryKey]
        public long Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
    }

    public class SlotTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        [Reference]
        public FacilityUseTable FacilityUseTable { get; set; }
        [ForeignKey(typeof(FacilityUseTable), OnDelete = "CASCADE")]
        public long FacilityUseId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long MaximumUses { get; set; }
        public long LeasedUses { get; set; }
        public long RemainingUses { get; set; }
        public decimal? Price { get; set; }
        public bool AllowCustomerCancellationFullRefund { get; set; }
        public RequiredStatusType? Prepayment { get; set; }
        public bool RequiresAttendeeValidation { get; set; }
        public bool RequiresApproval { get; set; }
        public bool RequiresAdditionalDetails { get; set; }
        public List<AdditionalDetailTypes> RequiredAdditionalDetails { get; set; }
        public TimeSpan? ValidFromBeforeStartDate { get; set; }
        public TimeSpan? LatestCancellationBeforeStartDate { get; set; }
        public bool AllowsProposalAmendment { get; set; }
    }

    public class FacilityUseTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; } // Provider
        public decimal LocationLat { get; set; }
        public decimal LocationLng { get; set; }
    }

    public class BookingPartnerTable
    {
        [PrimaryKey]
        public string ClientId { get; set; }
        public string Name { get; set; }
        public string ClientSecret { get; set; }
        public ClientModel ClientProperties { get; set; }
        public bool Registered { get; set; }
        public DateTime CreatedDate { get; set; }
        public string InitialAccessToken { get; set; }
        public DateTime InitialAccessTokenKeyValidUntil { get; set; }
        public bool BookingsSuspended { get; set; }
        public string Email { get; set; }
    }

    public class GrantTable
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string SubjectId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? Expiration { get; set; }
        public string Data { get; set; }
    }

    public class ClientModel
    {
        public string ClientUri { get; set; }

        public string LogoUri { get; set; }

        public string[] GrantTypes { get; set; }

        public string[] RedirectUris { get; set; }

        public string Scope { get; set; }
    }

    public static class DatabaseCreator
    {
        public static void CreateTables(OrmLiteConnectionFactory dbFactory)
        {
            using (var db = dbFactory.Open())
            {
                db.DropTable<SellerUserTable>();
                db.DropTable<OrderItemsTable>();
                db.DropTable<OccurrenceTable>();
                db.DropTable<OrderTable>();
                db.DropTable<ClassTable>();
                db.DropTable<SellerTable>();
                db.DropTable<FacilityUseTable>();
                db.DropTable<SlotTable>();
                db.DropTable<GrantTable>();
                db.DropTable<BookingPartnerTable>();
                db.CreateTable<GrantTable>();
                db.CreateTable<BookingPartnerTable>();
                db.CreateTable<SellerTable>();
                db.CreateTable<ClassTable>();
                db.CreateTable<OrderTable>();
                db.CreateTable<OccurrenceTable>();
                db.CreateTable<OrderItemsTable>();
                db.CreateTable<FacilityUseTable>();
                db.CreateTable<SlotTable>();
                db.CreateTable<SellerUserTable>();
            }
        }
    }
}
