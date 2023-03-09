using OpenActive.NET;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public class BookingFlowContext
    {
        public FlowStage Stage { get; internal set; }
        public OrderIdTemplate OrderIdTemplate { get; internal set; }
        public OrderIdComponents OrderId { get; internal set; }
        public TaxPayeeRelationship TaxPayeeRelationship { get; internal set; }
        public ILegalEntity Payer { get; internal set; }
        public ILegalEntity Seller { get; internal set; }
        public SimpleIdComponents SellerId { get; internal set; }
        public SimpleIdComponents CustomerAccountId { get; internal set; }
        public BrokerType BrokerRole { get; internal set; }
    }
}
