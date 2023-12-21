using System;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
    public class SlotTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        [Reference]
        public FacilityUseTable FacilityUseTable { get; set; }
        [ForeignKey(typeof(FacilityUseTable), OnDelete = "CASCADE")]
        public long FacilityUseId { get; set; }
        public long? IndividualFacilityUseId { get; set; }
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
}