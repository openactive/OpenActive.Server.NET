using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using System;
using System.Collections.Generic;

namespace OpenActive.Server.NET.StoreBooking
{
    public class StoreBookingEngineSettings
    {
        // TODO: Add check to ensure at least e-mail is always included in both of these (the only required field)
        public Func<Person, Person> CustomerPersonSupportedFields { get; set; } = p => new Person { Email = p.Email };
        public Func<Organization, Organization> CustomerOrganizationSupportedFields { get; set; } = o => new Organization { Email = o.Email };
        public Func<Organization, Organization> BrokerSupportedFields { get; set; } = o => new Organization {};
        public Func<Payment, Payment> PaymentSupportedFields { get; set; } = o => new Payment { };
        public BookingService BookingServiceDetails { get; set; }
        public Dictionary<IOpportunityStore, List<OpportunityType>> OpportunityStoreRouting { get; set; }
            = new Dictionary<IOpportunityStore, List<OpportunityType>>();
        public IOrderStore OrderStore { get; set; }
        public bool BusinessToConsumerTaxCalculation { get; set; }
        public bool BusinessToBusinessTaxCalculation { get; set; }
    }
}
