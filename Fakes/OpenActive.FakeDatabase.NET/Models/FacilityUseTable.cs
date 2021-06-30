using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
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
}