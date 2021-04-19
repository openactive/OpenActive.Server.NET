using System.Collections.Generic;
using System;
using System.Linq;
using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.CustomBooking;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IOrderItemContext
    {
        int Index { get; set; }
        IBookableIdComponents RequestBookableOpportunityOfferId { get; set; }
        OrderIdComponents ResponseOrderItemId { get; }
        OrderItem RequestOrderItem { get; set; }
        OrderItem ResponseOrderItem { get; }
        bool RequiresApproval { get; }
    }

    public class UnknownOrderItemContext : OrderItemContext<NullBookableIdComponents>
    {
        private UnknownOrderItemContext(int index, OrderItem orderItem)
        {
            this.Index = index;
            this.RequestBookableOpportunityOfferId = null;
            this.RequestOrderItem = orderItem;
            this.SetResponseOrderItemAsSkeleton();
        }

        public UnknownOrderItemContext(int index, OrderItem orderItem, OpenBookingError openBookingError) : this(index, orderItem)
        {
            this.AddError(openBookingError);
        }

        public UnknownOrderItemContext(int index, OrderItem orderItem, OpenBookingError openBookingError, string description) : this(index, orderItem)
        {
            this.AddError(openBookingError, description);
        }
    }

    public class OrderItemContext<TComponents> : IOrderItemContext where TComponents : IBookableIdComponents
    {
        public int Index { get; set; }
        public TComponents RequestBookableOpportunityOfferId { get; set; }
        IBookableIdComponents IOrderItemContext.RequestBookableOpportunityOfferId { get => this.RequestBookableOpportunityOfferId; set => this.RequestBookableOpportunityOfferId = (TComponents)value; }
        public OrderIdComponents ResponseOrderItemId { get; private set; }
        public OrderItem RequestOrderItem { get; set; }
        public OrderItem ResponseOrderItem { get; private set; }
        public bool RequiresApproval { get; set; } = false;

        public void AddError(OpenBookingError openBookingError)
        {
            if (ResponseOrderItem == null) throw new NotSupportedException("AddError cannot be used before SetResponseOrderItem.");
            if (ResponseOrderItem.Error == null) ResponseOrderItem.Error = new List<OpenBookingError>();
            ResponseOrderItem.Error.Add(openBookingError);
        }

        public void AddErrors(List<OpenBookingError> openBookingErrors)
        {
            if (ResponseOrderItem == null) throw new NotSupportedException("AddErrors cannot be used before SetResponseOrderItem.");
            if (ResponseOrderItem.Error == null) ResponseOrderItem.Error = new List<OpenBookingError>();
            ResponseOrderItem.Error.AddRange(openBookingErrors);
        }

        public void SetRequiresApproval()
        {
            RequiresApproval = true;
        }

        public void AddError(OpenBookingError openBookingError, string description)
        {
            if (openBookingError != null)
                openBookingError.Description = description;
            AddError(openBookingError);
        }

        public void SetOrderItemId(StoreBookingFlowContext flowContext, string orderItemId)
        {
            SetOrderItemId(flowContext, null, orderItemId);
        }

        public void SetOrderItemId(StoreBookingFlowContext flowContext, long orderItemId)
        {
            SetOrderItemId(flowContext, orderItemId, null);
        }

        private void SetOrderItemId(StoreBookingFlowContext flowContext, long? orderItemIdLong, string orderItemIdString)
        {
            if (flowContext == null) throw new ArgumentNullException(nameof(flowContext));
            if (ResponseOrderItem == null) throw new NotSupportedException("SetOrderItemId cannot be used before SetResponseOrderItem.");
            ResponseOrderItemId = new OrderIdComponents
            {
                uuid = flowContext.OrderId.uuid,
                OrderType = flowContext.OrderId.OrderType,
                OrderItemIdString = orderItemIdString,
                OrderItemIdLong = orderItemIdLong
            };
            ResponseOrderItem.Id = flowContext.OrderIdTemplate.RenderOrderItemId(ResponseOrderItemId);
        }

        public void SetResponseOrderItemAsSkeleton()
        {
            ResponseOrderItem = new OrderItem
            {
                Position = RequestOrderItem?.Position,
                AcceptedOffer = new Offer
                {
                    Id = RequestOrderItem?.AcceptedOffer.Object?.Id
                },
                OrderedItem = OrderCalculations.RenderOpportunityWithOnlyId(RequestOrderItem?.OrderedItem.Object?.Type, RequestOrderItem?.OrderedItem.Object?.Id)
            };
        }

        public void SetResponseOrderItem(OrderItem item, SellerIdComponents sellerId, StoreBookingFlowContext flowContext)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item?.OrderedItem.Object?.Id != RequestOrderItem?.OrderedItem.Object?.Id)
            {
                throw new ArgumentException("The Opportunity ID within the response OrderItem must match the request OrderItem");
            }
            if (item?.AcceptedOffer.Object?.Id != RequestOrderItem?.AcceptedOffer.Object?.Id)
            {
                throw new ArgumentException("The Offer ID within the response OrderItem must match the request OrderItem");
            }

            if (sellerId != flowContext.SellerId)
            {
                throw new OpenBookingException(new SellerMismatchError(), $"OrderItem at position {RequestOrderItem.Position} did not match the specified SellerId");
            }

            item.Position = RequestOrderItem?.Position;
            ResponseOrderItem = item;
        }

        public List<OpenBookingError> ValidateDetails(FlowStage flowStage)
        {
            if (ResponseOrderItem == null)
                throw new NotSupportedException("ValidateAttendeeDetails cannot be used before SetResponseOrderItem.");

            var validationErrors = new List<OpenBookingError>();
            var attendeeDetailsValidationResult = OrderCalculations.ValidateAttendeeDetails(ResponseOrderItem, flowStage);
            if (attendeeDetailsValidationResult != null)
                validationErrors.Add(attendeeDetailsValidationResult);

            var additionalDetailsValidationResult = OrderCalculations.ValidateAdditionalDetails(ResponseOrderItem, flowStage);
            validationErrors.AddRange(additionalDetailsValidationResult);

            return validationErrors;
        }
    }

    /// <summary>
    /// The StoreBookingEngine provides a more opinionated implementation of the Open Booking API on top of AbstractBookingEngine.
    /// This is designed to be quick to implement, but may not fit the needs of more complex systems.
    /// 
    /// It is not designed to be subclassed (it could be sealed?), but instead the implementer is encouraged
    /// to implement and provide an IOpenBookingStore on instantiation. 
    /// </summary>
    public class StoreBookingEngine : CustomBookingEngine
    {
        /// <summary>
        /// Simple constructor
        /// </summary>
        /// <param name="settings">settings are used exclusively by the AbstractBookingEngine</param>
        /// <param name="datasetSettings">datasetSettings are used exclusively by the DatasetSiteGenerator</param>
        /// <param name="storeBookingEngineSettings">storeBookingEngineSettings used exclusively by the StoreBookingEngine</param>
        public StoreBookingEngine(BookingEngineSettings settings, DatasetSiteGeneratorSettings datasetSettings, StoreBookingEngineSettings storeBookingEngineSettings) : base(settings, datasetSettings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (datasetSettings == null) throw new ArgumentNullException(nameof(datasetSettings));
            if (storeBookingEngineSettings == null) throw new ArgumentNullException(nameof(storeBookingEngineSettings));

            this.storeBookingEngineSettings = storeBookingEngineSettings;

            // TODO: Add test to ensure there are not two or more at FirstOrDefault step, in case of configuration error 
            this.storeRouting = storeBookingEngineSettings.OpportunityStoreRouting.Select(t => t.Value.Select(y => new
            {
                store = t.Key,
                opportunityType = y
            })).SelectMany(x => x.ToList()).GroupBy(g => g.opportunityType).ToDictionary(k => k.Key, v => v.Select(a => a.store).SingleOrDefault());

            // Setup each store with the relevant settings, including the relevant IdTemplate inferred from the config
            var storeConfig = storeBookingEngineSettings.OpportunityStoreRouting
                .ToDictionary(k => k.Key, v => v.Value.Select(y => base.OpportunityTemplateLookup[y]).Distinct().Single());
            foreach (var store in storeConfig)
            {
                store.Key.SetConfiguration(store.Value, settings.SellerIdTemplate);
            }
            storeBookingEngineSettings.OrderStore.SetConfiguration(settings.OrderIdTemplate, settings.SellerIdTemplate);

            // TODO: Check that OrderStore and all OpportunityStores are either sync or async, not a mix
        }

        private readonly Dictionary<OpportunityType, IOpportunityStore> storeRouting;
        private readonly StoreBookingEngineSettings storeBookingEngineSettings;

        protected async override Task<Event> InsertTestOpportunity(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller)
        {
            if (!storeRouting.ContainsKey(opportunityType))
                throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "Specified opportunity type is not configured as bookable in the StoreBookingEngine constructor.");

            return await storeRouting[opportunityType].CreateOpportunityWithinTestDataset(testDatasetIdentifier, opportunityType, criteria, seller);
        }

        protected async override Task DeleteTestDataset(string testDatasetIdentifier)
        {
            foreach (var store in storeRouting.Values)
            {
                await store.DeleteTestDataset(testDatasetIdentifier);
            }
        }

        protected async override Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdTemplate orderIdTemplate)
        {
            switch (simulateAction.Object.Value)
            {
                case Order order:
                    var orderIdComponents = orderIdTemplate.GetOrderIdComponents(null, order.Id);

                    if (orderIdComponents == null)
                    {
                        throw new OpenBookingException(new UnknownOrderError(), $"Order ID is not the expected format for a '{order.Type}': '{order.Id}'");
                    }

                    await storeBookingEngineSettings.OrderStore.TriggerTestAction(simulateAction, orderIdComponents);
                    break;

                case Event @event:
                    var opportunityIdComponents = base.ResolveOpportunityID(@event.Type, @event.Id);

                    if (opportunityIdComponents == null)
                    {
                        throw new OpenBookingException(new InvalidOpportunityOrOfferIdError(), $"Opportunity ID is not the expected format for a '{@event.Type}': '{@event.Id}'");
                    }

                    if (opportunityIdComponents.OpportunityType == null)
                    {
                        throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "OpportunityType must be configured for each IdComponent entry in the settings.");
                    }

                    var store = storeRouting[opportunityIdComponents.OpportunityType.Value];
                    if (store == null)
                    {
                        throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), $"Store is not defined for {opportunityIdComponents.OpportunityType.Value}");
                    }

                    await store.TriggerTestAction(simulateAction, opportunityIdComponents);
                    break;

                default:
                    throw new OpenBookingException(new OpenBookingError(), $"Object was not supplied");
            }
        }


        public async override Task ProcessCustomerCancellation(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds)
        {
            if (!await storeBookingEngineSettings.OrderStore.CustomerCancelOrderItems(orderId, sellerId, orderIdTemplate, orderItemIds))
            {
                throw new OpenBookingException(new UnknownOrderError(), "Order not found");
            }
        }

        public async override Task ProcessOrderProposalCustomerRejection(OrderIdComponents orderId, SellerIdComponents sellerId, OrderIdTemplate orderIdTemplate)
        {
            if (!await storeBookingEngineSettings.OrderStore.CustomerRejectOrderProposal(orderId, sellerId, orderIdTemplate))
            {
                throw new OpenBookingException(new UnknownOrderError(), "OrderProposal not found");
            }
        }

        protected async override Task<DeleteOrderResult> ProcessOrderDeletion(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            return await storeBookingEngineSettings.OrderStore.DeleteOrder(orderId, sellerId);
        }

        protected async override Task ProcessOrderQuoteDeletion(OrderIdComponents orderId, SellerIdComponents sellerId)
        {
            await storeBookingEngineSettings.OrderStore.DeleteLease(orderId, sellerId);
        }

        private static void CheckOrderIntegrity(Order requestOrder, Order responseOrder)
        {
            // If any capacity errors were returned from GetOrderItems, the booking must fail
            // https://www.openactive.io/open-booking-api/EditorsDraft/#order-creation-b
            if (responseOrder.OrderedItem.Any(i => i.Error != null && i.Error.Any(e => e != null && e.GetType() == typeof(OpportunityHasInsufficientCapacityError))))
            {
                throw new OpenBookingException(new OpportunityHasInsufficientCapacityError());
            }

            // If any lease capacity errors were returned from GetOrderItems, the booking must fail
            // https://www.openactive.io/open-booking-api/EditorsDraft/#order-creation-b
            if (responseOrder.OrderedItem.Any(i => i.Error != null && i.Error.Any(e => e != null && e.GetType() == typeof(OpportunityCapacityIsReservedByLeaseError))))
            {
                throw new OpenBookingException(new OpportunityCapacityIsReservedByLeaseError());
            }

            // If any other errors were returned from GetOrderItems, the booking must fail
            // https://www.openactive.io/open-booking-api/EditorsDraft/#order-creation-b
            if (responseOrder.OrderedItem.Any(x => x.Error != null && x.Error.Count > 0))
            {
                throw new OpenBookingException(new UnableToProcessOrderItemError(), string.Join(", ", responseOrder.OrderedItem.Where(x => x.Error != null).SelectMany(x => x.Error).Select(x => x.Name ?? "").ToList()));
            }

            // Throw error on payment due mismatch
            if (requestOrder.TotalPaymentDue?.Price != responseOrder.TotalPaymentDue?.Price)
            {
                throw new OpenBookingException(new TotalPaymentDueMismatchError());
            }

            // If no payment provided by broker, prepayment must either be required, or not specified with a nonzero price
            if (requestOrder.Payment == null && (
                    responseOrder.TotalPaymentDue?.Prepayment == RequiredStatusType.Required ||
                    responseOrder.TotalPaymentDue?.Price > 0 && responseOrder.TotalPaymentDue?.Prepayment == null))
            {
                throw new OpenBookingException(new MissingPaymentDetailsError(), "Orders with prepayment must have nonzero price.");
            }

            // If payment provided by broker, prepayment must not be unavailable and price must not be zero
            if (requestOrder.Payment != null && (
                    responseOrder.TotalPaymentDue?.Prepayment == RequiredStatusType.Unavailable ||
                    responseOrder.TotalPaymentDue?.Price == 0))
            {
                throw new OpenBookingException(new UnnecessaryPaymentDetailsError(), "Orders without prepayment must have zero price.");
            }

            // If a payment is provided, and has not thrown a previous error, a payment identifier is required
            if (requestOrder.Payment != null && requestOrder.Payment?.Identifier == null)
            {
                throw new OpenBookingException(new IncompletePaymentDetailsError(), "Payment must contain identifier for paid Order.");
            }
        }

        protected async override Task<Order> ProcessGetOrderStatus(OrderIdComponents orderId, SellerIdComponents sellerIdComponents, ILegalEntity seller)
        {
            // Get Order without OrderItems expanded
            var order = await storeBookingEngineSettings.OrderStore.GetOrderStatus(orderId, sellerIdComponents, seller);

            // Get flowContext from resulting Order, treating it like a request (which also validates it like a request)
            var flowContext = AugmentContextFromOrder(ValidateFlowRequest<Order>(orderId, sellerIdComponents, seller, FlowStage.OrderStatus, order), order);

            // Expand OrderItems based on the flowContext
            var (orderItemContexts, _) = await GetOrderItemContexts(order.OrderedItem, flowContext, null);

            // Maintain IDs and OrderItemStatus from GetOrderStatus that will have been overwritten by expansion
            foreach (var ctx in orderItemContexts)
            {
                ctx.ResponseOrderItem.Id = ctx.RequestOrderItem.Id;
                ctx.ResponseOrderItem.OrderItemStatus = ctx.RequestOrderItem.OrderItemStatus;
            }
            order.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

            // TODO: Should other properties be extracted from the flowContext for consistency, or do we trust the internals not to create excessive props?
            order.BookingService = flowContext.BookingService;
            return order;
        }

        public async override Task<Order> ProcessOrderCreationFromOrderProposal(OrderIdComponents orderId, OrderIdTemplate orderIdTemplate, ILegalEntity seller, SellerIdComponents sellerId, Order order)
        {
            if (!await storeBookingEngineSettings.OrderStore.CreateOrderFromOrderProposal(orderId, sellerId, order.OrderProposalVersion, order))
            {
                throw new OpenBookingException(new OrderProposalVersionOutdatedError());
            }
            return await ProcessGetOrderStatus(orderId, sellerId, seller);
        }

        private async Task<(List<IOrderItemContext>, List<OrderItemContextGroup>)> GetOrderItemContexts(List<OrderItem> sourceOrderItems, StoreBookingFlowContext flowContext, IStateContext stateContext)
        {
            // Create OrderItemContext for each OrderItem
            var orderItemContexts = sourceOrderItems.Select((orderItem, index) =>
            {
                // Error if this group of types is not recognised
                if (!base.IsOpportunityTypeRecognised(orderItem.OrderedItem.Object.Type))
                {
                    return new UnknownOrderItemContext(index, orderItem,
                        new UnknownOpportunityError(), $"The type of opportunity specified is not recognised: '{orderItem.OrderedItem.Object.Type}'.");
                }

                var idComponents = base.ResolveOpportunityID(orderItem.OrderedItem.Object.Type, orderItem.OrderedItem.Object.Id, orderItem.AcceptedOffer.Object.Id);
                if (idComponents == null)
                {
                    return new UnknownOrderItemContext(index, orderItem,
                        new InvalidOpportunityOrOfferIdError(), $"Opportunity and Offer ID pair are not in the expected format for a '{orderItem.OrderedItem.Object.Type}': '{orderItem.OrderedItem.Object.Id}' and '{orderItem.AcceptedOffer.Object.Id}'");
                }

                if (idComponents.OpportunityType == null)
                {
                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "OpportunityType must be configured for each IdComponent entry in the settings.");
                }

                // Create the relevant OrderItemContext using the specific type of the IdComponents returned
                Type type = typeof(OrderItemContext<>).MakeGenericType(idComponents.GetType());
                IOrderItemContext orderItemContext = (IOrderItemContext)Activator.CreateInstance(type);
                orderItemContext.Index = index;
                orderItemContext.RequestBookableOpportunityOfferId = idComponents;
                orderItemContext.RequestOrderItem = orderItem;

                return orderItemContext;

            }).ToList();

            // Group by OpportunityType for processing
            var orderItemGroupsTasks = orderItemContexts
                .Where(ctx => ctx.RequestBookableOpportunityOfferId != null)
                .GroupBy(ctx => ctx.RequestBookableOpportunityOfferId.OpportunityType.Value)

            // Get OrderItems first, to check no conflicts exist and that all items are valid
            // Resolve the ID of each OrderItem via a store
            .Select(async orderItemContextGroup =>
            {
                var opportunityType = orderItemContextGroup.Key;
                var orderItemContextsWithinGroup = orderItemContextGroup.ToList();
                var store = storeRouting[opportunityType];
                if (store == null)
                {
                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), $"Store is not defined for {opportunityType}");
                }

                // QUESTION: Should GetOrderItems occur within the transaction?
                // Currently this is optimised for the transaction to have minimal query coverage (i.e. write-only)

                await store.GetOrderItems(orderItemContextsWithinGroup, flowContext, stateContext);

                if (!orderItemContextsWithinGroup.TrueForAll(x => x.ResponseOrderItem != null))
                {
                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "Not all OrderItemContext have a ResponseOrderItem set. GetOrderItems must always call SetResponseOrderItem for each supplied OrderItemContext.");
                }

                if (!orderItemContextsWithinGroup.TrueForAll(x => x.ResponseOrderItem?.Error != null || (x.ResponseOrderItem?.AcceptedOffer.Object?.Price != null && x.ResponseOrderItem?.AcceptedOffer.Object?.PriceCurrency != null)))
                {
                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "Not all OrderItemContext have a ResponseOrderItem set with an AcceptedOffer containing both Price and PriceCurrency.");
                }

                // TODO: Implement error logic for all types of item errors based on the results of this

                return new OrderItemContextGroup
                {
                    OpportunityType = opportunityType,
                    Store = store,
                    OrderItemContexts = orderItemContextsWithinGroup
                };
            });
            var orderItemGroups = new List<OrderItemContextGroup>();
            orderItemGroups.AddRange(await Task.WhenAll(orderItemGroupsTasks));

            return (orderItemContexts, orderItemGroups);
        }

        private StoreBookingFlowContext AugmentContextFromOrder<TOrder>(BookingFlowContext request, TOrder order) where TOrder : Order, new()
        {
            StoreBookingFlowContext context = new StoreBookingFlowContext(request);

            // Reflect back only those customer fields that are supported
            switch (order.Customer)
            {
                case AuthenticatedPerson authenticatedPerson:
                    context.AuthenticatedCustomer = authenticatedPerson;
                    context.Customer = null;
                    break;

                case Person person:
                    context.Customer = storeBookingEngineSettings.CustomerPersonSupportedFields(person);
                    break;

                case Organization organization:
                    context.Customer = storeBookingEngineSettings.CustomerOrganizationSupportedFields(organization);
                    break;
            }

            // Throw error on missing AuthToken
            if (context.AuthenticatedCustomer != null)
            {
                if (context.AuthenticatedCustomer.AccessToken == null)
                    throw new OpenBookingException(new OpenBookingError(), "beta:CustomerAuthTokenMissingError");
            }

            // Throw error on incomplete broker details
            if (order.BrokerRole != BrokerType.NoBroker && (order.Broker == null || string.IsNullOrWhiteSpace(order.Broker.Name)))
            {
                throw new OpenBookingException(new IncompleteBrokerDetailsError());
            }

            // Throw error on Incomplete Order Item Error if OrderedItem or AcceptedOffer is null or their Urls don't match.
            if ((context.Stage == FlowStage.C1 || context.Stage == FlowStage.C2 || context.Stage == FlowStage.B) && order.OrderedItem.Any(orderItem => orderItem.OrderedItem.Object == null || orderItem.AcceptedOffer.Object == null || orderItem.OrderedItem.Object.Url != orderItem.AcceptedOffer.Object.Url))
            {
                throw new OpenBookingException(new IncompleteOrderItemError());
            }

            // Throw error on incomplete customer details if C2, P or B if Broker type is not ResellerBroker
            if (order.BrokerRole != BrokerType.ResellerBroker)
            {
                if (context.Stage != FlowStage.C1 && (context.Customer == null || context.Customer.IsPerson && string.IsNullOrWhiteSpace(context.Customer.Email)))
                {
                    throw new OpenBookingException(new IncompleteCustomerDetailsError());
                }
            }

            // Reflect back only those broker fields that are supported
            context.Broker = storeBookingEngineSettings.BrokerSupportedFields(order.Broker);

            // Reflect back only those broker fields that are supported
            context.Payment = order.Payment == null ? null : storeBookingEngineSettings.PaymentSupportedFields(order.Payment);

            // Add broker role to context for completeness
            context.BrokerRole = order.BrokerRole;

            // Get static BookingService fields from settings
            context.BookingService = storeBookingEngineSettings.BookingServiceDetails;

            return context;
        }

        public async override Task<TOrder> ProcessFlowRequest<TOrder>(BookingFlowContext request, TOrder order)
        {
            var context = AugmentContextFromOrder(request, order);

            // Runs before the flow starts, for both leasing and booking
            // Useful for transferring state between stages of the flow
            var stateContext = storeBookingEngineSettings.OrderStore.InitialiseFlow(context);

            var (orderItemContexts, orderItemGroups) = await GetOrderItemContexts(order.OrderedItem, context, stateContext);

            // Create a response Order based on the original order of the OrderItems in orderItemContexts
            TOrder responseGenericOrder = new TOrder
            {
                Id = context.OrderIdTemplate.RenderOrderId(context.OrderId),
                BrokerRole = context.BrokerRole,
                Broker = context.Broker,
                Seller = new ReferenceValue<ILegalEntity>(context.Seller),
                Customer = context.Customer,
                BookingService = context.BookingService,
                Payment = context.Payment,
                OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList()
            };

            // Add totals to the resulting Order
            OrderCalculations.AugmentOrderWithTotals(
                responseGenericOrder, context, storeBookingEngineSettings.BusinessToConsumerTaxCalculation, storeBookingEngineSettings.BusinessToBusinessTaxCalculation);

            switch (responseGenericOrder)
            {
                case OrderProposal responseOrderProposal:
                    if (context.Stage != FlowStage.P)
                        throw new OpenBookingException(new UnexpectedOrderTypeError());

                    CheckOrderIntegrity(order, responseOrderProposal);

                    // Proposal creation is atomic
                    using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.OrderStore.BeginOrderTransaction(context.Stage))
                    {
                        if (dbTransaction == null)
                        {
                            throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "A transaction is required for OrderProposal Creation at P, to ensure the integrity of the booking made.");
                        }

                        try
                        {
                            // Create the parent Order
                            string version;
                            OrderProposalStatus orderProposalStatus;
                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    (version, orderProposalStatus) = orderStoreSync.CreateOrderProposalSync(responseOrderProposal, context, stateContext, dbTransaction);
                                    break;

                                case IOrderStoreAsync orderStoreAsync:
                                    (version, orderProposalStatus) = await orderStoreAsync.CreateOrderProposalAsync(responseOrderProposal, context, stateContext, dbTransaction);
                                    break;
                                default: throw new ArgumentException("OrderStore not configured, either sync or async OrderStore must be configured");
                            }
                            responseOrderProposal.OrderProposalVersion = new Uri($"{responseOrderProposal.Id}/versions/{version}");
                            responseOrderProposal.OrderProposalStatus = orderProposalStatus;

                            // Book the OrderItems
                            foreach (var g in orderItemGroups)
                            {
                                {
                                    switch (g.Store)
                                    {
                                        case IOpportunityStoreSync opportunityStoreSync:
                                            opportunityStoreSync.ProposeOrderItemsSync(g.OrderItemContexts, context, stateContext, dbTransaction);
                                            break;
                                        case IOpportunityStoreAsync opportunityStoreAsync:
                                            await opportunityStoreAsync.ProposeOrderItemsAsync(g.OrderItemContexts, context, stateContext, dbTransaction);
                                            break;

                                    }
                                }

                                foreach (var ctx in g.OrderItemContexts)
                                {
                                    // Remove OrderItem Id that may have been added
                                    if (ctx.ResponseOrderItemId != null || ctx.ResponseOrderItem.Id != null)
                                    {
                                        throw new ArgumentException("SetOrderItemId must not be called for any OrderItemContext in ProposeOrderItems");
                                    }

                                    // Set the orderItemStatus to be https://openactive.io/OrderItemProposed (as it must always be so in the response of P)
                                    ctx.ResponseOrderItem.OrderItemStatus = OrderItemStatus.OrderItemProposed;
                                }
                            }

                            // Update this in case ResponseOrderItem was overwritten in Propose
                            responseOrderProposal.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    {
                                        orderStoreSync.UpdateOrderProposalSync(responseOrderProposal, context, stateContext, dbTransaction);
                                        break;
                                    }
                                case IOrderStoreAsync orderStoreAsync:
                                    await orderStoreAsync.UpdateOrderProposalAsync(responseOrderProposal, context, stateContext, dbTransaction);
                                    break;
                            }

                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Commit();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Commit();
                                        break;
                                }
                            }
                        }
                        catch
                        {
                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Rollback();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Rollback();
                                        break;
                                }
                            }
                            throw;
                        }
                    }
                    break;

                case OrderQuote responseOrderQuote:
                    if (!(context.Stage == FlowStage.C1 || context.Stage == FlowStage.C2))
                        throw new OpenBookingException(new UnexpectedOrderTypeError());

                    // If "payment" has been supplied unnecessarily, simply do not return it
                    if (responseOrderQuote.Payment != null && responseOrderQuote.TotalPaymentDue.Price.Value == 0)
                    {
                        responseOrderQuote.Payment = null;
                    }

                    // Note behaviour here is to lease those items that are available to be leased, and return errors for everything else
                    // Leasing is optimistic, booking is atomic
                    using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.OrderStore.BeginOrderTransaction(context.Stage))
                    {
                        try
                        {
                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    {
                                        responseOrderQuote.Lease = orderStoreSync.CreateLeaseSync(responseOrderQuote, context, stateContext, dbTransaction);
                                        break;
                                    }
                                case IOrderStoreAsync orderStoreAsync:
                                    responseOrderQuote.Lease = await orderStoreAsync.CreateLeaseAsync(responseOrderQuote, context, stateContext, dbTransaction);
                                    break;
                            }

                            // Lease the OrderItems, if a lease exists
                            if (responseOrderQuote.Lease != null)
                            {
                                foreach (var g in orderItemGroups)
                                {
                                    switch (g.Store)
                                    {
                                        case IOpportunityStoreSync opportunityStoreSync:
                                            opportunityStoreSync.LeaseOrderItemsSync(responseOrderQuote.Lease, g.OrderItemContexts, context, stateContext, dbTransaction);
                                            break;
                                        case IOpportunityStoreAsync opportunityStoreAsync:
                                            await opportunityStoreAsync.LeaseOrderItemsAsync(responseOrderQuote.Lease, g.OrderItemContexts, context, stateContext, dbTransaction);
                                            break;

                                    }
                                }
                            }

                            // Update this in case ResponseOrderItem was overwritten in Lease
                            responseOrderQuote.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                            // Note OrderRequiresApproval is only required during C1 and C2
                            responseOrderQuote.OrderRequiresApproval = orderItemContexts.Any(x => x.RequiresApproval);

                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    {
                                        orderStoreSync.UpdateLeaseSync(responseOrderQuote, context, stateContext, dbTransaction);
                                        break;
                                    }
                                case IOrderStoreAsync orderStoreAsync:
                                    await orderStoreAsync.UpdateLeaseAsync(responseOrderQuote, context, stateContext, dbTransaction);
                                    break;
                            }

                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Commit();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Commit();
                                        break;
                                }
                            }
                        }
                        catch
                        {
                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Rollback();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Rollback();
                                        break;
                                }
                            }
                            throw;
                        }
                    }
                    break;

                case Order responseOrder:
                    if (context.Stage != FlowStage.B)
                        throw new OpenBookingException(new UnexpectedOrderTypeError());

                    CheckOrderIntegrity(order, responseOrder);

                    // Booking is atomic
                    using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.OrderStore.BeginOrderTransaction(context.Stage))
                    {
                        if (dbTransaction == null)
                        {
                            throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "A transaction is required for booking at B, to ensure the integrity of the booking made.");
                        }

                        try
                        {
                            // Create the parent Order
                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    {
                                        orderStoreSync.CreateOrderSync(responseOrder, context, stateContext, dbTransaction);
                                        break;
                                    }
                                case IOrderStoreAsync orderStoreAsync:
                                    await orderStoreAsync.CreateOrderAsync(responseOrder, context, stateContext, dbTransaction);
                                    break;
                            }


                            // Book the OrderItems
                            foreach (var g in orderItemGroups)
                            {
                                switch (g.Store)
                                {
                                    case IOpportunityStoreSync opportunityStoreSync:
                                        opportunityStoreSync.BookOrderItemsSync(g.OrderItemContexts, context, stateContext, dbTransaction);
                                        break;
                                    case IOpportunityStoreAsync opportunityStoreAsync:
                                        await opportunityStoreAsync.BookOrderItemsAsync(g.OrderItemContexts, context, stateContext, dbTransaction);
                                        break;

                                }

                                foreach (var ctx in g.OrderItemContexts)
                                {
                                    // Check that OrderItem Id was added
                                    if (ctx.ResponseOrderItemId == null || ctx.ResponseOrderItem.Id == null)
                                    {
                                        throw new ArgumentException("SetOrderItemId must be called for each OrderItemContext in BookOrderItems");
                                    }

                                    // Set the orderItemStatus to be https://openactive.io/OrderConfirmed (as it must always be so in the response of B)
                                    ctx.ResponseOrderItem.OrderItemStatus = OrderItemStatus.OrderItemConfirmed;
                                }
                            }

                            // Update this in case ResponseOrderItem was overwritten in Book
                            responseOrder.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                            switch (storeBookingEngineSettings.OrderStore)
                            {
                                case IOrderStoreSync orderStoreSync:
                                    {
                                        orderStoreSync.UpdateOrderSync(responseOrder, context, stateContext, dbTransaction);
                                        break;
                                    }
                                case IOrderStoreAsync orderStoreAsync:
                                    await orderStoreAsync.UpdateOrderAsync(responseOrder, context, stateContext, dbTransaction);
                                    break;
                            }

                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Commit();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Commit();
                                        break;
                                }
                            }
                        }
                        catch
                        {
                            if (dbTransaction != null)
                            {
                                switch (dbTransaction)
                                {
                                    case IDatabaseTransactionSync dbTransactionSync:
                                        dbTransactionSync.Rollback();
                                        break;
                                    case IDatabaseTransactionAsync dbTransactionAsync:
                                        await dbTransactionAsync.Rollback();
                                        break;
                                }
                            }
                            throw;
                        }
                    }
                    break;

                default:
                    throw new OpenBookingException(new UnexpectedOrderTypeError());
            }

            return responseGenericOrder;
        }
    }

    internal class OrderItemContextGroup
    {
        public OpportunityType OpportunityType { get; set; }
        public IOpportunityStore Store { get; set; }
        public List<IOrderItemContext> OrderItemContexts { get; set; }
    }
}

