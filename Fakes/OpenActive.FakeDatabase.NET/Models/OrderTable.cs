using System;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
    [CompositeIndex(nameof(OrderModified), nameof(OrderId))]
    [CompositeIndex(nameof(OrderProposalModified), nameof(OrderId))]
    public class OrderTable
    {
        /**
         * Note string type is used for the OrderId instead of Guid type to allow for correct ordering of GUIDs for the RPDE feed
         */
        [PrimaryKey]
        public string OrderId { get; set; }

        public bool Deleted { get; set; }
        public DateTimeOffset OrderCreated { get; set; } = DateTimeOffset.Now;
        public long OrderModified { get; set; } = DateTimeOffset.Now.UtcTicks;
        public long OrderProposalModified { get; set; } = DateTimeOffset.Now.UtcTicks;
        public string ClientId { get; set; }

        [Reference]
        public SellerTable SellerTable { get; set; }
        [ForeignKey(typeof(SellerTable), OnDelete = "CASCADE")]
        public long SellerId { get; set; }
        public bool? CustomerIsOrganization { get; set; }
        public CustomerType CustomerType { get; set; }
        public BrokerRole BrokerRole { get; set; }
        public string BrokerName { get; set; }
        public Uri BrokerUrl { get; set; }
        public string BrokerTelephone { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerIdentifier { get; set; }
        public string CustomerGivenName { get; set; }
        public string CustomerFamilyName { get; set; }
        public string CustomerTelephone { get; set; }
        public string CustomerOrganizationName { get; set; }
        public string PaymentIdentifier { get; set; }
        public string PaymentName { get; set; }
        public string PaymentProviderId { get; set; }
        public string PaymentAccountId { get; set; }
        public decimal TotalOrderPrice { get; set; }
        public OrderMode OrderMode { get; set; }
        public DateTime LeaseExpires { get; set; }
        public FeedVisibility VisibleInOrdersFeed { get; set; }
        public FeedVisibility VisibleInOrderProposalsFeed { get; set; }
        public ProposalStatus? ProposalStatus { get; set; }
        public Guid? ProposalVersionId { get; set; }
    }
}