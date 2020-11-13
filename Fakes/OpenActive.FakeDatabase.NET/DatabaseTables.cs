﻿using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{
    public enum BrokerRole { AgentBroker, ResellerBroker, NoBroker }

    public enum BookingStatus { None, CustomerCancelled, SellerCancelled, Confirmed, Attended, Proposed }

    public enum ProposalStatus { AwaitingSellerConfirmation, SellerAccepted, SellerRejected, CustomerRejected }

    public enum OrderMode { Lease, Proposal, Booking }

    public enum RequiredStatusType { Required, Optional, Unavailable }

    [CompositeIndex(nameof(Modified), nameof(Id))]
    public abstract class Table
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
        public bool Deleted { get; set; }
        public long Modified { get; set; } = DateTimeOffset.Now.UtcTicks;
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
        public bool RequiresApproval { get; set; }
        public TimeSpan? ValidFromBeforeStartDate { get; set; }
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
        public string OpportunityJsonLdType { get; set; }
        public string OpportunityJsonLdId { get; set; }
        public string OfferJsonLdId { get; set; }
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
        public BookingStatus Status { get; set; }
        public string CancellationMessage { get; set; }
        public decimal Price { get; set; }
        public string PinCode {get; set;}
        public string ImageUrl { get; set; }
        public string BarCodeText { get; set; }
    }

    [CompositeIndex(nameof(Modified), nameof(OrderId))]
    public class OrderTable
    {
        [PrimaryKey]
        public string OrderId { get; set; }

        public bool Deleted { get; set; }
        public long Modified { get; set; } = DateTimeOffset.Now.UtcTicks;
        public string ClientId { get; set; }

        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
        public bool CustomerIsOrganization { get; set; }
        public BrokerRole BrokerRole { get; set; }
        public string BrokerName { get; set; }
        public string CustomerEmail { get; set; }
        public string PaymentIdentifier { get; set; }
        public decimal TotalOrderPrice { get; set; }
        public OrderMode OrderMode { get; set; }
        public DateTime LeaseExpires { get; set; }
        public bool VisibleInFeed { get; set; }
        public ProposalStatus? ProposalStatus { get; set; }
        public string ProposalVersionId { get; set; }
    }

    public class SellerTable
    {
        [PrimaryKey]
        public long Id { get; set; }
        //public string SellerId { get; set; }
        public string Name { get; set; }
        public bool IsIndividual { get; set; }
        public string Url { get; set; }
        public bool IsTaxGross { get; set; }
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
        public RequiredStatusType? Prepayment { get; set; }
        public bool RequiresApproval { get; set; }
        public TimeSpan? ValidFromBeforeStartDate { get; set; }
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
    }

    public class BookingPartnerTable
    {
        [PrimaryKey]
        [AutoIncrement]
        public long BookingPartnerId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public ClientModel ClientProperties { get; set; }
        public bool Registered { get; set; }
        public DateTime CreatedDate { get; set; }
        public string RegistrationKey { get; set; }
        public DateTime RegistrationKeyValidUntil { get; set; }
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
        public string ClientName { get; set; }

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
            }
        }
    }
}
