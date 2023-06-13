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
        public decimal LocationLat { get; set; }
        public decimal LocationLng { get; set; }
        //private string IndividualFacilityUseString { get; set; }
        //[Ignore]
        //public List<IndividualFacilityUse> IndividualFacilityUses
        //{
        //    get
        //    {
        //        return JsonConvert.DeserializeObject<List<IndividualFacilityUse>>(IndividualFacilityUseString);
        //    }
        //    set
        //    {
        //        IndividualFacilityUseString = JsonConvert.SerializeObject(IndividualFacilityUses);
        //    }
        //}
        public List<IndividualFacilityUse> IndividualFacilityUses { get; set; }
    }
}