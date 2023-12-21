using System.Collections.Generic;
using Newtonsoft.Json;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
    public class IndividualFacilityUse
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string SportActivityLocationName { get; set; }

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
        public long PlaceId { get; set; }
        public List<IndividualFacilityUse> IndividualFacilityUses { get; set; }
    }
}