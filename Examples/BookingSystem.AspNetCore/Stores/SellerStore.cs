using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class AcmeSellerStore : SellerStore
    {
        private readonly bool _useSingleSellerMode;

        /// <summary>
        /// Example constructor that can set state from EngineConfig.
        /// This is not required for an actual implementation.
        /// </summary>
        /// <param name="useSingleSellerMode">Single seller mode feature-flag</param>
        public AcmeSellerStore(bool useSingleSellerMode)
        {
            _useSingleSellerMode = useSingleSellerMode;
        }

        // If the Seller is not found, simply return null to generate the correct Open Booking error
        protected override async ValueTask<ILegalEntity> GetSeller(SimpleIdComponents simpleIdComponents)
        {
            // Note both examples are shown below to demonstrate options available. Only one block of the if statement below is required for an actual implementation.
            if (_useSingleSellerMode)
            {
                // For Single Seller booking systems, no ID will be available from simpleIdComponents, and this data should instead come from your configuration table
                return new Organization
                {
                    Id = RenderSingleSellerId(),
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
                    },
                    TermsOfService = new List<Terms>
                    {
                        new PrivacyPolicy
                        {
                            Name = "Privacy Policy",
                            Url = new Uri("https://example.com/privacy.html"),
                            RequiresExplicitConsent = false
                        }
                    },
                    IsOpenBookingAllowed = true,
                };
            }

            // Otherwise it may be looked up based on supplied simpleIdComponents which are extracted from the sellerId.
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var seller = db.SingleById<SellerTable>(simpleIdComponents.IdLong);
                if (seller == null)
                {
                    // Seller not found
                    return null;
                }

                return seller.IsIndividual ? new Person
                {
                    Id = RenderSellerId(new SimpleIdComponents { IdLong = seller.Id }),
                    Name = seller.Name,
                    TaxMode = seller.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
                    LegalName = seller.Name,
                    // Note these strings should be populated from the database
                    Address = new PostalAddress
                    {
                        StreetAddress = "1 Fake Place",
                        AddressLocality = "Faketown",
                        AddressRegion = "Oxfordshire",
                        PostalCode = "OX1 1AA",
                        AddressCountry = "GB"
                    },
                    IsOpenBookingAllowed = true,
                } : (ILegalEntity)new Organization
                {
                    Id = RenderSellerId(new SimpleIdComponents { IdLong = seller.Id }),
                    Name = seller.Name,
                    TaxMode = seller.IsTaxGross ? TaxMode.TaxGross : TaxMode.TaxNet,
                    LegalName = seller.Name,
                    // Note these strings should be populated from the database
                    Address = new PostalAddress
                    {
                        StreetAddress = "1 Hidden Gem",
                        AddressLocality = "Another town",
                        AddressRegion = "Oxfordshire",
                        PostalCode = "OX1 1AA",
                        AddressCountry = "GB"
                    },
                    TermsOfService = new List<Terms>
                    {
                        new PrivacyPolicy
                        {
                            Name = "Privacy Policy",
                            Url = new Uri("https://example.com/privacy.html"),
                            RequiresExplicitConsent = false
                        }
                    },
                    IsOpenBookingAllowed = true,
                };

            }
        }
    }
}