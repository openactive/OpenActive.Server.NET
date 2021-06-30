using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
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
}