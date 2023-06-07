using System;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
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
        [ForeignKey(typeof(OccurrenceTable), OnDelete = "NO ACTION")] // SQL Server seems to have a problem with chained cascading
        public long? OccurrenceId { get; set; }
        [Reference]
        public SlotTable SlotTable { get; set; }
        [ForeignKey(typeof(SlotTable), OnDelete = "NO ACTION")] // SQL Server seems to have a problem with chained cascading
        public long? SlotId { get; set; }
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
        public string AttendeeString { get; set; }
        public string AdditionalDetailsString { get; set; }
    }
}