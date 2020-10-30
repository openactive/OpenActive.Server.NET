using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;

namespace BookingSystem
{
    public class AcmeSellerStore : SellerStore
    {
        // Example constructor that can set state from EngineConfig. This is not required for an actual implementation.
        private bool UseSingleSellerMode;
        public AcmeSellerStore(bool UseSingleSellerMode)
        {
            this.UseSingleSellerMode = UseSingleSellerMode;
        }

        // If the Seller is not found, simply return null to generate the correct Open Booking error
        protected override ILegalEntity GetSeller(SellerIdComponents sellerIdComponents)
        {
            // Note both examples are shown below to demonstrate options available. Only one block of the if statement below is required for an actual implementation.
            if (UseSingleSellerMode)
            {
                // For Single Seller booking systems, no ID will be available from sellerIdComponents, and this data should instead come from your configuration table
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
                    }
                };
            }
            else
            {
                // Otherwise it may be looked up based on supplied sellerIdComponents which are extacted from the sellerId.
                using (var db = FakeBookingSystem.Database.Mem.Database.Open())
                {
                    var seller = db.SingleById<SellerTable>(sellerIdComponents.SellerIdLong);
                    if (seller != null)
                    {
                        return seller.IsIndividual ? new Person
                        {
                            Id = RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
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
                            Id = RenderSellerId(new SellerIdComponents { SellerIdLong = seller.Id }),
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
                            },
                            TermsOfService = new List<Terms>
                            {
                                new PrivacyPolicy
                                {
                                    Name = "Privacy Policy",
                                    Url = new Uri("https://example.com/privacy.html"),
                                    RequiresExplicitConsent = false
                                }
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
