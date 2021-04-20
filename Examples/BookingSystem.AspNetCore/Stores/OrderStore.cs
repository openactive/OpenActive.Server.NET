using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class OrderStateContext : IStateContext
    {
    }

    public class AcmeOrderStore : OrderStore<OrderTransaction, OrderStateContext>
    {
        private readonly AppSettings _appSettings;

        public AcmeOrderStore(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        /// <summary>
        /// Initiate customer cancellation for the specified OrderItems
        /// Note sellerId will always be null in Single Seller mode
        /// </summary>
        /// <returns>True if Order found, False if Order not found</returns>
        public async override Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds)
        {
            try
            {
                return FakeBookingSystem.Database.CancelOrderItems(
                    orderId.ClientId,
                    sellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    orderId.uuid,
                    orderItemIds.Where(x => x.OrderItemIdLong.HasValue).Select(x => x.OrderItemIdLong.Value).ToList(), true);
            }
            catch (InvalidOperationException ex)
            {
                throw new OpenBookingException(new CancellationNotPermittedError(), ex.Message);
            }
        }

        /// <summary>
        /// Reject specified OrderProposal
        /// Note sellerId will always be null in Single Seller mode
        /// </summary>
        /// <returns>True if OrderProposal found, False if OrderProposal not found</returns>
        public override async Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate)
        {
            return FakeBookingSystem.Database.RejectOrderProposal(orderId.ClientId, sellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */, orderId.uuid, true);
        }

        public async override Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents idComponents)
        {
            switch (simulateAction)
            {
                case SellerAcceptOrderProposalSimulateAction _:
                    if (idComponents.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    if (!FakeBookingSystem.Database.AcceptOrderProposal(idComponents.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRejectOrderProposalSimulateAction _:
                    if (idComponents.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    if (!FakeBookingSystem.Database.RejectOrderProposal(null, null, idComponents.uuid, false))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRequestedCancellationWithMessageSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.CancelOrderItems(null, null, idComponents.uuid, null, false, true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRequestedCancellationSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.CancelOrderItems(null, null, idComponents.uuid, null, false))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessCodeUpdateSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.UpdateAccess(idComponents.uuid, updateAccessCode: true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessPassUpdateSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.UpdateAccess(idComponents.uuid, updateAccessPass: true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case OpportunityAttendanceUpdateSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.UpdateOpportunityAttendance(idComponents.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case CustomerNoticeSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.AddCustomerNotice(idComponents.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case ReplacementSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.ReplaceOrderOpportunity(idComponents.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessChannelUpdateSimulateAction _:
                    if (idComponents.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!FakeBookingSystem.Database.UpdateAccess(idComponents.uuid, updateAccessChannel: true))
                    {
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

        private static BrokerRole BrokerTypeToBrokerRole(BrokerType brokerType)
        {
            return brokerType == BrokerType.AgentBroker
                    ? BrokerRole.AgentBroker
                    : brokerType == BrokerType.ResellerBroker
                        ? BrokerRole.ResellerBroker
                        : BrokerRole.NoBroker;
        }

        private static BrokerType BrokerRoleToBrokerType(BrokerRole brokerRole)
        {
            return brokerRole == BrokerRole.AgentBroker
                    ? BrokerType.AgentBroker
                    : brokerRole == BrokerRole.ResellerBroker
                        ? BrokerType.ResellerBroker
                        : BrokerType.NoBroker;
        }

        public async override ValueTask<Lease> CreateLease(
            OrderQuote responseOrderQuote,
            StoreBookingFlowContext flowContext,
            OrderStateContext stateContext,
            OrderTransaction databaseTransaction,
            bool useAsync)
        {
            if (_appSettings.FeatureFlags.PaymentReconciliationDetailValidation && ReconciliationMismatch(flowContext))
                throw new OpenBookingException(new InvalidPaymentDetailsError(), "Payment reconciliation details do not match");

            // Note if no lease support, simply return null always here instead
            if (flowContext.Stage != FlowStage.C1 && flowContext.Stage != FlowStage.C2)
                return null;

            // TODO: Make the lease duration configurable
            var leaseExpires = DateTimeOffset.UtcNow + new TimeSpan(0, 5, 0);
            var brokerRole = BrokerTypeToBrokerRole(flowContext.BrokerRole ?? BrokerType.NoBroker);

            var result = useAsync ?
            await FakeDatabase.AddLeaseAsync(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                brokerRole,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                leaseExpires,
                databaseTransaction.FakeDatabaseTransaction)
             : FakeDatabase.AddLease(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                brokerRole,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                leaseExpires,
                databaseTransaction.FakeDatabaseTransaction);

            if (!result)
                throw new OpenBookingException(new OrderAlreadyExistsError());

            return new Lease
            {
                LeaseExpires = leaseExpires
            };
        }

        public async override ValueTask UpdateLease(
            OrderQuote responseOrderQuote,
            StoreBookingFlowContext flowContext,
            OrderStateContext stateContext,
            OrderTransaction databaseTransaction,
            bool useAsync)
        {
            // Does nothing at the moment
        }

        public async override Task DeleteLease(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            // Note if no lease support, simply do nothing here
            FakeBookingSystem.Database.DeleteLease(
                orderId.ClientId,
                orderId.uuid,
                sellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */
                );
        }

        public async override ValueTask CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction, bool useAsync)
        {
            if (_appSettings.FeatureFlags.PaymentReconciliationDetailValidation && responseOrder.TotalPaymentDue.Price > 0 && ReconciliationMismatch(flowContext))
                throw new OpenBookingException(new InvalidPaymentDetailsError(), "Payment reconciliation details do not match");

            if (!responseOrder.TotalPaymentDue.Price.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "TotalPaymentDue must have a price set");

            var result = useAsync ?
                await FakeDatabase.AddOrderAsync(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                flowContext.Payment?.Identifier,
                responseOrder.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                null,
                null)
                : FakeDatabase.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                flowContext.Payment?.Identifier,
                responseOrder.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                null,
                null);

            if (!result) throw new OpenBookingException(new OrderAlreadyExistsError());
        }

        public async override ValueTask UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction, bool useAsync)
        {
            // Does nothing at the moment
        }

        public async override ValueTask<(string, OrderProposalStatus)> CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction, bool useAsync)
        {
            if (!responseOrderProposal.TotalPaymentDue.Price.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "Price must be set on TotalPaymentDue");

            var version = Guid.NewGuid().ToString();
            var result = useAsync ?
                await FakeDatabase.AddOrderAsync(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                flowContext.Payment?.Identifier,
                responseOrderProposal.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                version,
                ProposalStatus.AwaitingSellerConfirmation)
                : FakeDatabase.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.SellerId.SellerIdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                flowContext.Customer?.Email,
                flowContext.Payment?.Identifier,
                responseOrderProposal.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                version,
                ProposalStatus.AwaitingSellerConfirmation);

            if (!result)
                throw new OpenBookingException(new OrderAlreadyExistsError());

            return (version, OrderProposalStatus.AwaitingSellerConfirmation);
        }

        public async override ValueTask UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction, bool useAsync)
        {
            // Does nothing at the moment
        }

        public async override Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            var result = FakeBookingSystem.Database.DeleteOrder(
                orderId.ClientId,
                orderId.uuid,
                sellerId.SellerIdLong ?? null /* Small hack to allow use of FakeDatabase when in Single Seller mode */);
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

        public async override Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SellerIdComponents sellerId, Uri orderProposalVersion, Order order)
        {
            // TODO more elegantly extract version UUID from orderProposalVersion (probably much further up the stack?)
            var version = orderProposalVersion.ToString().Split('/').Last();

            var result = FakeBookingSystem.Database.BookOrderProposal(
                orderId.ClientId,
                sellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                orderId.uuid,
                version);
            // TODO return enum to allow errors cases to be handled in the engine
            switch (result)
            {
                case FakeDatabaseBookOrderProposalResult.OrderSuccessfullyBooked:
                    return true;
                case FakeDatabaseBookOrderProposalResult.OrderProposalVersionOutdated:
                    return false;
                case FakeDatabaseBookOrderProposalResult.OrderProposalNotAccepted:
                    throw new OpenBookingException(new OrderCreationFailedError(), "OrderProposal has not been accepted by the Seller");
                case FakeDatabaseBookOrderProposalResult.OrderWasNotFound:
                    throw new OpenBookingException(new UnknownOrderError());
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"Unexpected FakeDatabaseDeleteOrderResult: {result}");
            }
        }

        public static Order CreateOrderFromOrderMode(OrderMode orderMode, Uri orderId, string proposalVersionId, ProposalStatus? proposalStatus)
        {
            switch (orderMode)
            {
                case OrderMode.Booking:
                    return new Order();
                case OrderMode.Lease:
                    return new OrderQuote();
                case OrderMode.Proposal:
                    var o = new OrderProposal();
                    o.OrderProposalVersion = new Uri($"{orderId}/versions/{proposalVersionId}");
                    o.OrderProposalStatus = proposalStatus == ProposalStatus.AwaitingSellerConfirmation ? OrderProposalStatus.AwaitingSellerConfirmation :
                        proposalStatus == ProposalStatus.CustomerRejected ? OrderProposalStatus.CustomerRejected :
                        proposalStatus == ProposalStatus.SellerAccepted ? OrderProposalStatus.SellerAccepted :
                        proposalStatus == ProposalStatus.SellerRejected ? OrderProposalStatus.SellerRejected : (OrderProposalStatus?)null;
                    return o;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderMode));
            }
        }

        public static Order RenderOrderFromDatabaseResult(Uri orderId, OrderTable dbOrder, List<OrderItem> orderItems)
        {
            var order = CreateOrderFromOrderMode(dbOrder.OrderMode, orderId, dbOrder.ProposalVersionId, dbOrder.ProposalStatus);
            order.Id = orderId;
            order.Identifier = new Guid(dbOrder.OrderId);
            order.TotalPaymentDue = new PriceSpecification
            {
                Price = dbOrder.TotalOrderPrice,
                PriceCurrency = "GBP"
            };
            order.OrderedItem = orderItems;

            return order;
        }

        public async override Task<Order> GetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerId, ILegalEntity seller)
        {
            var (getOrderResult, dbOrder, dbOrderItems) = await FakeBookingSystem.Database.GetOrderAndOrderItemsAsync(
                orderId.ClientId,
                sellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                orderId.uuid);
            if (getOrderResult == FakeDatabaseGetOrderResult.OrderWasNotFound) throw new OpenBookingException(new UnknownOrderError());

            var orderIdUri = RenderOrderId(dbOrder.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : dbOrder.OrderMode == OrderMode.Lease ? OrderType.OrderQuote : OrderType.Order, dbOrder.OrderId);
            var orderItems = dbOrderItems.Select((orderItem) => new OrderItem
            {
                Id = dbOrder.OrderMode == OrderMode.Booking ? RenderOrderItemId(OrderType.Order, dbOrder.OrderId, orderItem.Id) : null,
                AcceptedOffer = new Offer
                {
                    Id = new Uri(orderItem.OfferJsonLdId),
                },
                OrderedItem = RenderOpportunityWithOnlyId(orderItem.OpportunityJsonLdType, new Uri(orderItem.OpportunityJsonLdId)),
                OrderItemStatus =
                            orderItem.Status == BookingStatus.Confirmed ? OrderItemStatus.OrderItemConfirmed :
                            orderItem.Status == BookingStatus.CustomerCancelled ? OrderItemStatus.CustomerCancelled :
                            orderItem.Status == BookingStatus.SellerCancelled ? OrderItemStatus.SellerCancelled :
                            orderItem.Status == BookingStatus.Attended ? OrderItemStatus.CustomerAttended :
                            orderItem.Status == BookingStatus.Proposed ? OrderItemStatus.OrderItemProposed : (OrderItemStatus?)null
            }).ToList();
            var order = RenderOrderFromDatabaseResult(orderIdUri, dbOrder, orderItems);

            // These additional properties that are only available in the Order Status endpoint
            order.Seller = new ReferenceValue<ILegalEntity>(seller);
            order.Broker = new Organization
            {
                Name = dbOrder.BrokerName
            };
            order.BrokerRole = BrokerRoleToBrokerType(dbOrder.BrokerRole);
            order.Customer = new Person
            {
                Email = dbOrder.CustomerEmail
            };

            return order;
        }

        private bool ReconciliationMismatch(StoreBookingFlowContext flowContext)
        {
            // MissingPaymentDetailsError is handled by OpenActive.Server.NET, so ignoring empty payment details here allows the exception to be thrown by the booking engine.
            if (flowContext.Payment == null)
                return false;

            return flowContext.Payment.AccountId != _appSettings.Payment.AccountId || flowContext.Payment.PaymentProviderId != _appSettings.Payment.PaymentProviderId;
        }

        public override IDatabaseTransaction BeginOrderTransaction(FlowStage stage)
        {
            return new OrderTransaction();
        }
    }
}