using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Collections.Generic;

namespace OpenActive.Server.NET.StoreBooking
{
    //TODO: Refactor to inherrit from BookingFlowContext (using constructor to copy params? Use Automapper?)
    public class StoreBookingFlowContext : BookingFlowContext
    {
        public StoreBookingFlowContext(BookingFlowContext bookingFlowContext)
        {
            if (bookingFlowContext == null) throw new ArgumentNullException(nameof(bookingFlowContext));
            base.Stage = bookingFlowContext.Stage;
            base.OrderIdTemplate = bookingFlowContext.OrderIdTemplate;
            base.OrderId = bookingFlowContext.OrderId;
            base.TaxPayeeRelationship = bookingFlowContext.TaxPayeeRelationship;
            base.Payer = bookingFlowContext.Payer;
            base.Seller = bookingFlowContext.Seller;
            base.SellerId = bookingFlowContext.SellerId;
            base.BrokerRole = bookingFlowContext.BrokerRole;
        }

        public ILegalEntity Customer { get; internal set; }
        public Organization Broker { get; internal set; }
        public BookingService BookingService { get; internal set; }
        public Payment Payment { get; internal set; }
        public List<IOrderItemContext> OrderItemContexts { get; internal set; }
    }
}
