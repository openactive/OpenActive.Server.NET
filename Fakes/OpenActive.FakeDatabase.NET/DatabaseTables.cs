using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace OpenActive.FakeDatabase.NET
{

    public enum BrokerRole { AgentBroker, ResellerBroker, NoBroker }

    public enum BookingStatus { CustomerCancelled, SellerCancelled, Confirmed, Attended }


    public abstract class Table
    {
        [PrimaryKey]
        [AutoIncrement]
        [Alias("RpdeId")]
        public long Id { get; set; }
        public bool Deleted { get; set; } = false;
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
        public long OccurrenceId { get; set; }
        public BookingStatus Status { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderTable : Table
    {
        public string ClientId { get; set; }
        public string OrderId { get; set; }
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
        public bool IsLease { get; set; }
        public DateTime LeaseExpires { get; set; }
        public bool VisibleInFeed { get; set; }
    }

    public class SellerTable : Table
    {
        public string Name { get; set; }
        public bool IsIndividual { get; set; }
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
                db.CreateTable<SellerTable>();
                db.CreateTable<ClassTable>();
                db.CreateTable<OrderTable>();
                db.CreateTable<OccurrenceTable>();
                db.CreateTable<OrderItemsTable>();
            }
        }
    }
}
