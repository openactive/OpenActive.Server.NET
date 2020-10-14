using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;

namespace OpenActive.Server.NET.StoreBooking
{
    //TODO: Refactor to inherrit from BookingFlowContext (using constructor to copy params? Use Automapper?)
    public class StoreBookingFlowContext : BookingFlowContext
    {
        public StoreBookingFlowContext(BookingFlowContext bookingFlowContext)
        {
            if (bookingFlowContext == null) throw new ArgumentNullException(nameof(bookingFlowContext));
            Stage = bookingFlowContext.Stage;
            OrderIdTemplate = bookingFlowContext.OrderIdTemplate;
            OrderId = bookingFlowContext.OrderId;
            TaxPayeeRelationship = bookingFlowContext.TaxPayeeRelationship;
            Payer = bookingFlowContext.Payer;
            Seller = bookingFlowContext.Seller;
            SellerId = bookingFlowContext.SellerId;
        }

        public ILegalEntity Customer { get; internal set; }
        public AuthenticatedPerson AuthenticatedCustomer { get; internal set; }
        public Organization Broker { get; internal set; }
        public BookingService BookingService { get; internal set; }
        public BrokerType? BrokerRole { get; internal set; }
        public Payment Payment { get; internal set; }
    }
}
