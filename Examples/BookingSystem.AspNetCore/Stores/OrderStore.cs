using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BookingSystem
{
    public class OrderStateContext : IStateContext
    {

    }

    public class AcmeOrderStore : OrderStore<OrderTransaction, OrderStateContext>
    {
        /// <summary>
        /// Initiate customer cancellation for the specified OrderItems
        /// Note sellerId will always be null in Single Seller mode
        /// </summary>
        /// <returns>True if Order found, False if Order not found</returns>
        public override bool CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds)
        {
            //throw new OpenBookingException(new CancellationNotPermittedError());
            return FakeBookingSystem.Database.CancelOrderItem(orderId.ClientId, sellerId.SellerIdLong ?? null  /* Hack to allow this to work in Single Seller mode too */, orderId.uuid, orderItemIds.Select(x => x.OrderItemIdLong.Value).ToList(), true);
        }

        /// <summary>
        /// Reject specified OrderProposal
        /// Note sellerId will always be null in Single Seller mode
        /// </summary>
        /// <returns>True if OrderProposal found, False if OrderProposal not found</returns>
        public override bool CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate)
        {
            return FakeBookingSystem.Database.RejectOrderProposal(orderId.ClientId, sellerId.SellerIdLong ?? null  /* Hack to allow this to work in Single Seller mode too */, orderId.uuid, true);
        }

        public override void TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents)
        {
            switch (simulateAction)
            {
                case ReplacementSimulateAction _:
                    if (idComponents.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    if (!FakeBookingSystem.Database.AcceptOrderProposal(idComponents.uuid)) {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
            }
        }

        public override OrderStateContext Initialise(StoreBookingFlowContext flowContext)
        {
            // Runs before the flow starts, for both leasing and booking
            // Useful for transferring state between stages of the flow
            return new OrderStateContext();
        }

        public override Lease CreateLease(OrderQuote responseOrderQuote, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Note if no lease support, simply return null always here instead

            // In this example leasing is only supported at C2
            if (flowContext.Stage == FlowStage.C2)
            {
                // TODO: Make the lease duration configurable
                var leaseExpires = DateTimeOffset.UtcNow + new TimeSpan(0, 5, 0);

                var result = databaseTransaction.Database.AddLease(
                    flowContext.OrderId.ClientId,
                    flowContext.OrderId.uuid,
                    flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                    flowContext.Broker.Name,
                    flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                    flowContext.Customer.Email,
                    leaseExpires,
                    databaseTransaction?.Transaction
                    );

                if (!result) throw new OpenBookingException(new OrderAlreadyExistsError());

                return new Lease
                {
                    LeaseExpires = leaseExpires
                };
            }
            else
            {
                return null;
            }
        }

        public override void DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            // Note if no lease support, simply do nothing here
            FakeBookingSystem.Database.DeleteLease(orderId.ClientId, orderId.uuid, sellerId.SellerIdLong.Value);
        }

        public override void CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            var result = databaseTransaction.Database.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer.Email,
                flowContext.Payment?.Identifier,
                responseOrder.TotalPaymentDue.Price.Value,
                databaseTransaction.Transaction,
                null);

            if (!result) throw new OpenBookingException(new OrderAlreadyExistsError());
        }

        public override void CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            var result = databaseTransaction.Database.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer.Email,
                flowContext.Payment?.Identifier,
                responseOrderProposal.TotalPaymentDue.Price.Value,
                databaseTransaction.Transaction,
                Guid.NewGuid().ToString());

            if (!result) throw new OpenBookingException(new OrderAlreadyExistsError());
        }

        public override DeleteOrderResult DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            var result = FakeBookingSystem.Database.DeleteOrder(orderId.ClientId, orderId.uuid, sellerId.SellerIdLong ?? null /* Small hack to allow use of FakeDatabase when in Single Seller mode */);
            switch (result)
            {
                case FakeDatabaseDeleteOrderResult.OrderSuccessfullyDeleted:
                // "OrderWasAlreadyDeleted" is being treated as a success because the order did
                // exist - This maintains idempotency as requests that follow a successful request
                // will still return a 2xx.
                case FakeDatabaseDeleteOrderResult.OrderWasAlreadyDeleted:
                    return DeleteOrderResult.OrderSuccessfullyDeleted;
                case FakeDatabaseDeleteOrderResult.OrderWasNotFound:
                    return DeleteOrderResult.OrderDidNotExist;
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"Unexpected FakeDatabaseDeleteOrderResult: {result}");
            }
        }

        public override void UpdateLease(OrderQuote responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Runs after the transaction is committed
        }

        public override void UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Runs after the transaction is committed
        }

        public override void UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Runs after the transaction is committed
        }

        public override bool CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order)
        {
            // TODO extract version UUID from orderProposalVersion (probably much further up the stack?)
            var result = FakeBookingSystem.Database.BookOrderProposal(orderId.ClientId, sellerId.SellerIdLong ?? null  /* Hack to allow this to work in Single Seller mode too */, orderId.uuid, orderProposalVersion.ToString());
            // TODO return enum to allow errors cases to be handled in the engine
            switch (result)
            {
                case FakeDatabaseBookOrderProposalResult.OrderSuccessfullyBooked:
                    return true;
                case FakeDatabaseBookOrderProposalResult.OrderProposalVersionOutdated:
                    return false;
                case FakeDatabaseBookOrderProposalResult.OrderWasNotFound:
                    throw new OpenBookingException(new UnknownOrderError());
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"Unexpected FakeDatabaseDeleteOrderResult: {result}");
            }
        }

        public Order CreateOrderFromOrderMode(OrderMode orderMode)
        {
            switch (orderMode)
            {
                case OrderMode.Booking:
                    return new Order();
                case OrderMode.Lease:
                    return new OrderQuote();
                case OrderMode.Proposal:
                    return new OrderProposal();
                default:
                    throw new ArgumentOutOfRangeException("Unrecognised OrderMode");
            }
        }

        //TODO return Order
        public override Order GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var order = db.Single<OrderTable>(x => x.ClientId == orderId.ClientId && x.OrderId == orderId.uuid && !x.Deleted);
                var orderItems = db.Select<OrderItemsTable>(x => x.ClientId == orderId.ClientId && x.OrderId == orderId.uuid);

                // TODO: Ensure the appropriate type is created
                var o = CreateOrderFromOrderMode(order.OrderMode);
                o.Id = this.RenderOrderId(OrderType.Order, order.OrderId);
                o.Identifier = order.OrderId;
                o.Seller = seller;

                // Todo take these from database (and check whether Customer should be included from a GDPR point of view?!)
                o.BrokerRole = BrokerType.AgentBroker;
                o.Broker = new Organization
                {
                    Name = "Temp broker"
                };
                o.Customer = new Person
                {
                    Email = "temp@example.com"
                };

                o.TotalPaymentDue = new PriceSpecification
                {
                    Price = order.TotalOrderPrice,
                    PriceCurrency = "GBP"
                };
                o.OrderedItem = orderItems.Select((orderItem) => new OrderItem
                {
                    Id = this.RenderOrderItemId(OrderType.Order, order.OrderId, orderItem.Id),
                    AcceptedOffer = new Offer
                    {
                        Id = new Uri(orderItem.OfferJsonLdId)
                    },
                    OrderedItem = RenderOpportunityWithOnlyId(orderItem.OpportunityJsonLdType, new Uri(orderItem.OpportunityJsonLdId)),
                    OrderItemStatus =
                        orderItem.Status == BookingStatus.Confirmed ? OrderItemStatus.OrderItemConfirmed :
                        orderItem.Status == BookingStatus.CustomerCancelled ? OrderItemStatus.CustomerCancelled :
                        orderItem.Status == BookingStatus.SellerCancelled ? OrderItemStatus.SellerCancelled :
                        orderItem.Status == BookingStatus.Attended ? OrderItemStatus.CustomerAttended : 
                        orderItem.Status == BookingStatus.Proposed ? OrderItemStatus.OrderItemProposed : (OrderItemStatus?)null

                }).ToList();
                return o;
            }
        }

        protected override OrderTransaction BeginOrderTransaction(FlowStage stage)
        {
            if (stage != FlowStage.C1)
            {
                return new OrderTransaction();
            }
            else
            {
                return null;
            }
        }
    }
}
