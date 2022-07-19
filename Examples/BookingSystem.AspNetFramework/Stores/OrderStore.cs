using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class OrderStateContext : IStateContext
    {
        // OrderStateContext will be disposed at the end of the flow
        public void Dispose()
        {
        }
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
        public override async Task<bool> CustomerCancelOrderItems(OrderIdComponents orderId, SimpleIdComponents sellerId, List<OrderIdComponents> orderItemIds)
        {
            try
            {
                return await FakeBookingSystem.Database.CancelOrderItems(
                    orderId.ClientId,
                    sellerId.IdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
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
        public override async Task<bool> CustomerRejectOrderProposal(OrderIdComponents orderId, SimpleIdComponents sellerId)
        {
            return await FakeBookingSystem.Database.RejectOrderProposal(orderId.ClientId, sellerId.IdLong ?? null /* Hack to allow this to work in Single Seller mode too */, orderId.uuid, true);
        }

        public override async Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdComponents orderId)
        {
            switch (simulateAction)
            {
                case SellerAcceptOrderProposalSimulateAction _:
                    if (orderId.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    if (!await FakeBookingSystem.Database.AcceptOrderProposal(orderId.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerAmendOrderProposalSimulateAction _:
                    if (orderId.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    var version = Guid.NewGuid();
                    if (!await FakeBookingSystem.Database.AmendOrderProposal(orderId.uuid, version))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRejectOrderProposalSimulateAction _:
                    if (orderId.OrderType != OrderType.OrderProposal)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected OrderProposal");
                    }
                    if (!await FakeBookingSystem.Database.RejectOrderProposal(null, null, orderId.uuid, false))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRequestedCancellationWithMessageSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.CancelOrderItems(null, null, orderId.uuid, null, false, true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case SellerRequestedCancellationSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.CancelOrderItems(null, null, orderId.uuid, null, false))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessCodeUpdateSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.UpdateAccess(orderId.uuid, updateAccessCode: true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessPassUpdateSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.UpdateAccess(orderId.uuid, updateAccessPass: true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AttendeeAttendedSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.UpdateOpportunityAttendance(orderId.uuid, true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AttendeeAbsentSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.UpdateOpportunityAttendance(orderId.uuid, false))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case CustomerNoticeSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.AddCustomerNotice(orderId.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case ReplacementSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.ReplaceOrderOpportunity(orderId.uuid))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
                case AccessChannelUpdateSimulateAction _:
                    if (orderId.OrderType != OrderType.Order)
                    {
                        throw new OpenBookingException(new UnexpectedOrderTypeError(), "Expected Order");
                    }
                    if (!await FakeBookingSystem.Database.UpdateAccess(orderId.uuid, updateAccessChannel: true))
                    {
                        throw new OpenBookingException(new UnknownOrderError());
                    }
                    break;
            }
        }

        public override ValueTask<OrderStateContext> CreateOrderStateContext(StoreBookingFlowContext flowContext)
        {
            // Useful for transferring state between stages of the flow
            return new ValueTask<OrderStateContext>(new OrderStateContext());
        }

        public override ValueTask Initialise(StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {
            // Runs before the flow starts, for both leasing and booking
            // Simply remove this method if it is not required
            return new ValueTask();
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

        public override async ValueTask<Lease> CreateLease(
            OrderQuote responseOrderQuote,
            StoreBookingFlowContext flowContext,
            OrderStateContext stateContext,
            OrderTransaction databaseTransaction)
        {
            if (_appSettings.FeatureFlags.PaymentReconciliationDetailValidation && ReconciliationMismatch(flowContext))
                throw new OpenBookingException(new InvalidPaymentDetailsError(), "Payment reconciliation details do not match");

            // Note if no lease support, simply return null always here instead
            if (flowContext.Stage != FlowStage.C1 && flowContext.Stage != FlowStage.C2)
                return null;

            // TODO: Make the lease duration configurable
            var leaseExpires = DateTimeOffset.UtcNow + new TimeSpan(0, 5, 0);
            var brokerRole = BrokerTypeToBrokerRole(flowContext.BrokerRole);

            var result = await FakeDatabase.AddLease(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                brokerRole,
                flowContext.Broker.Name,
                flowContext.Broker.Url,
                flowContext.Broker.Telephone,
                flowContext.SellerId.IdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
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

        public override ValueTask UpdateLease(
            OrderQuote responseOrderQuote,
            StoreBookingFlowContext flowContext,
            OrderStateContext stateContext,
            OrderTransaction databaseTransaction)
        {
            // Does nothing at the moment
            return new ValueTask();
        }

        public override async Task DeleteLease(OrderIdComponents orderId, SimpleIdComponents sellerId)
        {
            // Note if no lease support, simply do nothing here
            await FakeBookingSystem.Database.DeleteLease(
                orderId.ClientId,
                orderId.uuid,
                sellerId.IdLong ?? null /* Hack to allow this to work in Single Seller mode too */
                );
        }

        public override async ValueTask CreateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            if (_appSettings.FeatureFlags.PaymentReconciliationDetailValidation && responseOrder.TotalPaymentDue.Price > 0 && ReconciliationMismatch(flowContext))
                throw new OpenBookingException(new InvalidPaymentDetailsError(), "Payment reconciliation details do not match");

            var customerType = flowContext.Customer == null ? CustomerType.None : (flowContext.Customer.IsOrganization ? CustomerType.Organization : CustomerType.Person);
            var result = await FakeDatabase.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.Broker.Url,
                flowContext.Broker.Telephone,
                flowContext.SellerId.IdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                customerType == CustomerType.None ? null : flowContext.Customer.Email,
                customerType,
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? flowContext.Customer.Name : null),
                customerType == CustomerType.None ? null : flowContext.Customer.Identifier.HasValue ? flowContext.Customer.Identifier.Value.ToString() : null,
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.GivenName),
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.FamilyName),
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.Telephone),
                flowContext.Payment?.Identifier,
                flowContext.Payment?.Name,
                flowContext.Payment?.PaymentProviderId,
                flowContext.Payment?.AccountId,
                responseOrder.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                null,
                null);

            if (!result) throw new OpenBookingException(new OrderAlreadyExistsError());
        }

        public override ValueTask UpdateOrder(Order responseOrder, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Does nothing at the moment
            return new ValueTask();
        }

        public override async ValueTask<(Guid, OrderProposalStatus)> CreateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            if (!responseOrderProposal.TotalPaymentDue.Price.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "Price must be set on TotalPaymentDue");

            var version = Guid.NewGuid();
            var customerType = flowContext.Customer == null ? CustomerType.None : (flowContext.Customer.GetType() == typeof(Organization) ? CustomerType.Organization : CustomerType.Person);
            var result = await FakeDatabase.AddOrder(
                flowContext.OrderId.ClientId,
                flowContext.OrderId.uuid,
                flowContext.BrokerRole == BrokerType.AgentBroker ? BrokerRole.AgentBroker : flowContext.BrokerRole == BrokerType.ResellerBroker ? BrokerRole.ResellerBroker : BrokerRole.NoBroker,
                flowContext.Broker.Name,
                flowContext.Broker.Url,
                flowContext.Broker.Telephone,
                flowContext.SellerId.IdLong ?? null, // Small hack to allow use of FakeDatabase when in Single Seller mode
                customerType == CustomerType.None ? null : flowContext.Customer.Email,
                customerType,
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? flowContext.Customer.Name : null),
                customerType == CustomerType.None ? null : flowContext.Customer.Identifier.HasValue ? flowContext.Customer.Identifier.Value.ToString() : null,
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.GivenName),
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.FamilyName),
                customerType == CustomerType.None ? null : (customerType == CustomerType.Organization ? null : flowContext.Customer.Telephone),
                flowContext.Payment?.Identifier,
                flowContext.Payment?.Name,
                flowContext.Payment?.PaymentProviderId,
                flowContext.Payment?.AccountId,
                responseOrderProposal.TotalPaymentDue.Price.Value,
                databaseTransaction.FakeDatabaseTransaction,
                version,
                ProposalStatus.AwaitingSellerConfirmation);

            if (!result)
                throw new OpenBookingException(new OrderAlreadyExistsError());

            return (version, OrderProposalStatus.AwaitingSellerConfirmation);
        }

        public override ValueTask UpdateOrderProposal(OrderProposal responseOrderProposal, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Does nothing at the moment
            return new ValueTask();
        }

        public override async Task<DeleteOrderResult> DeleteOrder(OrderIdComponents orderId, SimpleIdComponents sellerId)
        {
            var result = await FakeBookingSystem.Database.DeleteOrder(
                orderId.ClientId,
                orderId.uuid,
                sellerId.IdLong ?? null /* Small hack to allow use of FakeDatabase when in Single Seller mode */);
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

        public override async Task<bool> CreateOrderFromOrderProposal(OrderIdComponents orderId, SimpleIdComponents sellerId, Uri orderProposalVersion, Order order)
        {
            // TODO more elegantly extract version UUID from orderProposalVersion (probably much further up the stack?)
            var version = new Guid(orderProposalVersion.ToString().Split('/').Last());

            var result = await FakeBookingSystem.Database.BookOrderProposal(
                orderId.ClientId,
                sellerId.IdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
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

        public static Order CreateOrderFromOrderMode(OrderMode orderMode, Uri orderId, Guid? proposalVersionId, ProposalStatus? proposalStatus)
        {
            switch (orderMode)
            {
                case OrderMode.Booking:
                    return new Order();
                case OrderMode.Lease:
                    return new OrderQuote();
                case OrderMode.Proposal:
                    var o = new OrderProposal();
                    o.OrderProposalVersion = new Uri($"{orderId}/versions/{proposalVersionId.ToString()}");
                    o.OrderProposalStatus = proposalStatus == ProposalStatus.AwaitingSellerConfirmation ? OrderProposalStatus.AwaitingSellerConfirmation :
                        proposalStatus == ProposalStatus.CustomerRejected ? OrderProposalStatus.CustomerRejected :
                        proposalStatus == ProposalStatus.SellerAccepted ? OrderProposalStatus.SellerAccepted :
                        proposalStatus == ProposalStatus.SellerRejected ? OrderProposalStatus.SellerRejected : (OrderProposalStatus?)null;
                    return o;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderMode));
            }
        }

        public static Order RenderOrderFromDatabaseResult(Uri orderId, OrderTable dbOrder, bool prepaymentAlwaysRequired, List<OrderItem> orderItems)
        {
            var order = CreateOrderFromOrderMode(dbOrder.OrderMode, orderId, dbOrder.ProposalVersionId, dbOrder.ProposalStatus);
            order.Id = orderId;
            order.Identifier = new Guid(dbOrder.OrderId);
            order.TotalPaymentDue = new PriceSpecification
            {
                Price = dbOrder.TotalOrderPrice,
                PriceCurrency = "GBP",
                OpenBookingPrepayment = prepaymentAlwaysRequired ? null : OrderCalculations.GetRequiredStatusType(orderItems)
            };
            order.OrderedItem = orderItems;

            return order;
        }

        public override async Task<Order> GetOrderStatus(OrderIdComponents orderId, SimpleIdComponents sellerId, ILegalEntity seller)
        {
            var (getOrderResult, dbOrder, dbOrderItems) = await FakeBookingSystem.Database.GetOrderAndOrderItems(
                orderId.ClientId,
                sellerId.IdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                orderId.uuid);
            if (getOrderResult == FakeDatabaseGetOrderResult.OrderWasNotFound) throw new OpenBookingException(new UnknownOrderError());

            var orderIdUri = RenderOrderId(dbOrder.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : dbOrder.OrderMode == OrderMode.Lease ? OrderType.OrderQuote : OrderType.Order, new Guid(dbOrder.OrderId));
            var orderItems = dbOrderItems.Select((orderItem) =>
            {
                return new OrderItem
                {
                    Id = dbOrder.OrderMode != OrderMode.Lease ? RenderOrderItemId(OrderType.Order, new Guid(dbOrder.OrderId), orderItem.Id) : null,
                    AcceptedOffer = new Offer
                    {
                        Id = orderItem.OfferJsonLdId,
                        Price = orderItem.Price
                    },
                    OrderedItem = orderItem.OpportunityJsonLdId,
                    OrderItemStatus =
                                orderItem.Status == BookingStatus.Confirmed ? OrderItemStatus.OrderItemConfirmed :
                                orderItem.Status == BookingStatus.CustomerCancelled ? OrderItemStatus.CustomerCancelled :
                                orderItem.Status == BookingStatus.SellerCancelled ? OrderItemStatus.SellerCancelled :
                                orderItem.Status == BookingStatus.Attended ? OrderItemStatus.AttendeeAttended :
                                orderItem.Status == BookingStatus.Absent ? OrderItemStatus.AttendeeAbsent : (OrderItemStatus?)null,
                    Attendee = orderItem.AttendeeString != null ? OpenActiveSerializer.Deserialize<Person>(orderItem.AttendeeString) : null,
                    OrderItemIntakeFormResponse = orderItem.AdditionalDetailsString != null ? OpenActiveSerializer.DeserializeList<PropertyValue>(orderItem.AdditionalDetailsString) : null,
                };
            }).ToList();
            var order = RenderOrderFromDatabaseResult(orderIdUri, dbOrder, _appSettings.FeatureFlags.PrepaymentAlwaysRequired, orderItems);

            // Map AcceptedOffer from object to IdReference
            var mappedOrderItems = order.OrderedItem.Select((orderItem) => new OrderItem
            {
                Id = orderItem.Id,
                AcceptedOffer = orderItem.AcceptedOffer.Object.Id,
                OrderedItem = orderItem.OrderedItem,
                OrderItemStatus = orderItem.OrderItemStatus,
                Attendee = orderItem.Attendee,
                OrderItemIntakeFormResponse = orderItem.OrderItemIntakeFormResponse,
            }).ToList();
            order.OrderedItem = mappedOrderItems;

            // These additional properties that are only available in the Order Status endpoint or B after P+A
            order.Seller = new ReferenceValue<ILegalEntity>(seller);
            order.BrokerRole = BrokerRoleToBrokerType(dbOrder.BrokerRole);
            order.Broker = order.BrokerRole == BrokerType.NoBroker ? null : new Organization
            {
                Name = dbOrder.BrokerName,
                Url = dbOrder.BrokerUrl,
                Telephone = dbOrder.BrokerTelephone
            };
            order.BrokerRole = BrokerRoleToBrokerType(dbOrder.BrokerRole);
            if (dbOrder.CustomerType == CustomerType.Organization)
            {
                order.Customer = new Organization
                {
                    Email = dbOrder.CustomerEmail,
                    Name = dbOrder.CustomerOrganizationName,
                };
            }
            else if (dbOrder.CustomerType == CustomerType.Person)
            {
                order.Customer = new Person
                {
                    Email = dbOrder.CustomerEmail,
                    Identifier = dbOrder.CustomerIdentifier,
                    GivenName = dbOrder.CustomerGivenName,
                    FamilyName = dbOrder.CustomerFamilyName,
                    Telephone = dbOrder.CustomerTelephone,
                };

            }
            // Payment Identifier is mandatory for non-free sessions
            if (dbOrder.PaymentIdentifier != null)
            {
                order.Payment = new Payment
                {
                    Identifier = dbOrder.PaymentIdentifier,
                    PaymentProviderId = dbOrder.PaymentProviderId,
                    AccountId = dbOrder.PaymentAccountId,
                    Name = dbOrder.PaymentName
                };
            }

            return order;
        }

        private bool ReconciliationMismatch(StoreBookingFlowContext flowContext)
        {
            // MissingPaymentDetailsError is handled by OpenActive.Server.NET, so ignoring empty payment details here allows the exception to be thrown by the booking engine.
            if (flowContext.Payment == null)
                return false;

            return flowContext.Payment.AccountId != _appSettings.Payment.AccountId || flowContext.Payment.PaymentProviderId != _appSettings.Payment.PaymentProviderId;
        }

        public override ValueTask<IDatabaseTransaction> BeginOrderTransaction(FlowStage stage)
        {
            // Returning new ValueTask<IDatabaseTransaction>() for sync completion
            return new ValueTask<IDatabaseTransaction>(new OrderTransaction());

            // Note that this method can also be made async for async completion
        }
    }
}