using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System.Linq;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class AcmeSellerStore : SellerStore
    {
        // If the Seller is not found, simply return null to generate the correct Open Booking error
        protected override async Task<ILegalEntity> GetSeller(SellerIdComponents sellerIdComponents)
        {
            // Note both examples are shown below to demonstrate options available. Only one block of the if statement below is required.
            if (sellerIdComponents.SellerIdLong == null && sellerIdComponents.SellerIdString == null)
            {

                // For Single Seller booking systems, no ID will be available from sellerIdComponents, and this data should instead come from your configuration table
                return new Organization
                {
                    Id = this.RenderSingleSellerId(),
                    Name = "Test Seller",
                    TaxMode = TaxMode.TaxGross,
                    LegalName = "Test Seller Ltd",
                    Address = new PostalAddress
                    {
                        StreetAddress = "1 Hidden Gem",
                        AddressLocality = "Another town",
                        AddressRegion = "Oxfordshire",
                        PostalCode = "OX1 1AA",
                        AddressCountry = "GB"
                    }
                };

            }
            else
            {

                // Otherwise it may be looked up based on supplied sellerIdComponents which are extacted from the sellerId.
                using (var db = await FakeBookingSystem.Database.Mem.Database.OpenAsync())
                {
                    var seller = await db.SingleByIdAsync<SellerTable>(sellerIdComponents.SellerIdLong);
                    if (seller != null)
                    {
                        return seller.IsIndividual ? (ILegalEntity)new Person
                        {
                            Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
                            Name = seller.Name,
                            TaxMode = TaxMode.TaxGross,
                            LegalName = seller.Name,
                            Address = new PostalAddress
                            {
                                StreetAddress = "1 Fake Place",
                                AddressLocality = "Faketown",
                                AddressRegion = "Oxfordshire",
                                PostalCode = "OX1 1AA",
                                AddressCountry = "GB"
                            }
                        } : (ILegalEntity)new Organization
                        {
                            Id = this.RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
                            Name = seller.Name,
                            TaxMode = TaxMode.TaxGross,
                            LegalName = seller.Name,
                            Address = new PostalAddress
                            {
                                StreetAddress = "1 Hidden Gem",
                                AddressLocality = "Another town",
                                AddressRegion = "Oxfordshire",
                                PostalCode = "OX1 1AA",
                                AddressCountry = "GB"
                            }
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}
