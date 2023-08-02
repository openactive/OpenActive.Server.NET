using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
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
        public long PlaceId { get; set; }
        public AttendanceMode AttendanceMode { get; set; }
        public bool AllowsProposalAmendment { get; set; }
    }
}