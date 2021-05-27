using System;
using System.Collections.Generic;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem
{
    public static class StoreHelper
    {
        public static List<TaxChargeSpecification> GetUnitTaxSpecification(BookingFlowContext flowContext, AppSettings appSettings, decimal? price)
        {
            switch (flowContext.TaxPayeeRelationship)
            {
                case TaxPayeeRelationship.BusinessToBusiness when appSettings.Payment.TaxCalculationB2B:
                case TaxPayeeRelationship.BusinessToConsumer when appSettings.Payment.TaxCalculationB2C:
                    return new List<TaxChargeSpecification>
                    {
                        new TaxChargeSpecification
                        {
                            Name = "VAT at 20%",
                            Price = price * (decimal?)0.2,
                            PriceCurrency = "GBP",
                            Rate = (decimal?)0.2
                        }
                    };
                case TaxPayeeRelationship.BusinessToBusiness when !appSettings.Payment.TaxCalculationB2B:
                case TaxPayeeRelationship.BusinessToConsumer when !appSettings.Payment.TaxCalculationB2C:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
