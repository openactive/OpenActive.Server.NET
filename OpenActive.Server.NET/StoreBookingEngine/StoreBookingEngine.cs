using System.Collections.Generic;
using System;
using System.Linq;
using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.OpenBookingHelper.Extensions;
using OpenActive.Server.NET.CustomBooking;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IOrderItemContext
    {
        int Index { get; }
        IBookableIdComponents RequestBookableOpportunityOfferId { get; }
        OrderIdComponents ResponseOrderItemId { get; }
        OrderItem RequestOrderItem { get; }
        OrderItem ResponseOrderItem { get; }
        bool RequiresApproval { get; }
        bool IsSkeleton { get; }
        IOrderItemCustomContext CustomContext { get; }

        void AddError(OpenBookingError openBookingError);
        void AddError(OpenBookingError openBookingError, string description);

        void AddErrors(List<OpenBookingError> openBookingErrors);
        bool HasErrors { get; }
        void SetRequiresApproval();
        void SetCustomContext(IOrderItemCustomContext customContext);
    }

    /// <summary>
    /// Useful for passing state through the flow
    /// </summary>
    public interface IOrderItemCustomContext
    {
    }

    public class UnknownOrderItemContext : OrderItemContext<NullBookableIdComponents>
    {
        private UnknownOrderItemContext(int index, OrderItem orderItem) : base(index, null, orderItem)
        {
            this.SetResponseOrderItemAsSkeleton();
        }

        internal UnknownOrderItemContext(int index, OrderItem orderItem, OpenBookingError openBookingError) : this(index, orderItem)
        {
            this.AddError(openBookingError);
        }

        internal UnknownOrderItemContext(int index, OrderItem orderItem, OpenBookingError openBookingError, string description) : this(index, orderItem)
        {
            this.AddError(openBookingError, description);
        }
    }

    public class OrderItemContext<TComponents> : IOrderItemContext where TComponents : IBookableIdComponents
    {
        internal OrderItemContext(int index, IBookableIdComponents idComponents, OrderItem orderItem)
        {
            Index = index;
            RequestBookableOpportunityOfferId = (TComponents)idComponents;
            RequestOrderItem = orderItem;
        }

        public int Index { get; }
        public TComponents RequestBookableOpportunityOfferId { get; }
        IBookableIdComponents IOrderItemContext.RequestBookableOpportunityOfferId { get => this.RequestBookableOpportunityOfferId; }
        public OrderIdComponents ResponseOrderItemId { get; private set; }
        public OrderItem RequestOrderItem { get; }
        public bool IsSkeleton { get; private set; } = false;
        public OrderItem ResponseOrderItem { get; private set; }
        public bool RequiresApproval { get; private set; } = false;
        public IOrderItemCustomContext CustomContext { get; private set; }


        private List<OpenBookingError> Errors = null;

        public bool HasErrors {
            get {
                return Errors?.Count > 0;
            }
        }

        public void AddError(OpenBookingError openBookingError)
        {
            if (Errors == null)
            {
                Errors = new List<OpenBookingError>();
                if (ResponseOrderItem != null) ResponseOrderItem.Error = Errors;
            }
            Errors.Add(openBookingError);
        }

        public void AddErrors(List<OpenBookingError> openBookingErrors)
        {
            if (Errors == null)
            {
                Errors = new List<OpenBookingError>();
                if (ResponseOrderItem != null) ResponseOrderItem.Error = Errors;
            }
            Errors.AddRange(openBookingErrors);
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

        public void SetCustomContext(IOrderItemCustomContext customContext)
        {
            CustomContext = customContext;
        }

        public void SetOrderItemId(StoreBookingFlowContext flowContext, string orderItemId)
        {
            SetOrderItemId(flowContext, null, orderItemId, null);
        }

        public void SetOrderItemId(StoreBookingFlowContext flowContext, Guid orderItemId)
        {
            SetOrderItemId(flowContext, null, null, orderItemId);
        }

        public void SetOrderItemId(StoreBookingFlowContext flowContext, long orderItemId)
        {
            SetOrderItemId(flowContext, orderItemId, null, null);
        }

        private void SetOrderItemId(StoreBookingFlowContext flowContext, long? orderItemIdLong, string orderItemIdString, Guid? orderItemIdGuid)
        {
            if (flowContext == null) throw new ArgumentNullException(nameof(flowContext));
            if (ResponseOrderItem == null) throw new NotSupportedException("SetOrderItemId cannot be used before SetResponseOrderItem.");

            ResponseOrderItemId = new OrderIdComponents
            {
                uuid = flowContext.OrderId.uuid,
                OrderType = OrderType.Order, // All OrderItems that have an @id are of canonical type Order
                OrderItemIdString = orderItemIdString,
                OrderItemIdLong = orderItemIdLong,
                OrderItemIdGuid = orderItemIdGuid
            };
            ResponseOrderItem.Id = flowContext.OrderIdTemplate.RenderOrderItemId(ResponseOrderItemId);
        }

        public void SetResponseOrderItemAsSkeleton()
        {
            IsSkeleton = true;
            ResponseOrderItem = new OrderItem
            {
                Position = RequestOrderItem?.Position,
                AcceptedOffer = RequestOrderItem.AcceptedOffer,
                OrderedItem = RequestOrderItem.OrderedItem,
                Error = Errors
            };
        }

        public void SetResponseOrderItem(OrderItem item, SimpleIdComponents sellerId, StoreBookingFlowContext flowContext)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var requestOrderItemId = RequestOrderItem?.OrderedItem.IdReference;
            if (requestOrderItemId == null)
            {
                throw new OpenBookingException(new InternalLibraryError(), "Request must include an orderedItem @id for the OrderItem");
            }
            if (item?.OrderedItem.Object?.Id != requestOrderItemId)
            {
                throw new OpenBookingException(new InternalLibraryError(), "The Opportunity @id within the response OrderItem must match the request OrderItem");
            }
            var requestAcceptedOfferId = RequestOrderItem?.AcceptedOffer.IdReference;
            if (requestAcceptedOfferId == null)
            {
                //throw new OpenBookingException(new InternalLibraryError(), "Request must include an acceptedOffer for the OrderItem");
            }
            if (item?.AcceptedOffer.Object?.Id != requestAcceptedOfferId)
            {
                //throw new OpenBookingException(new InternalLibraryError(), "The Offer ID within the response OrderItem must match the request OrderItem");
            }

            if (sellerId != flowContext.SellerId)
            {
                throw new OpenBookingException(new SellerMismatchError(), $"OrderItem at position {RequestOrderItem.Position} did not match the specified SellerId");
            }

            if (item?.Error != null)
            {
                throw new OpenBookingException(new InternalLibraryError(), "Error property must not be set on OrderItem passed to SetResponseOrderItem");
            }

            if (item?.AcceptedOffer.Object?.Price == 0 && !(item?.AcceptedOffer.Object?.OpenBookingPrepayment == null || item?.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Unavailable))
            {
                throw new OpenBookingException(new InternalLibraryError(), "OpenBookingPrepayment must be set to null or Unavailable for free opportunities.");
            }

            if (item.OrderedItem.Object.EndDate.NullableValue < DateTimeOffset.Now)
            {
                AddError(new OpportunityOfferPairNotBookableError(), "Opportunities in the past are not bookable");
            }

            if (item.OrderedItem.Object.EventStatus == Schema.NET.EventStatusType.EventCancelled)
            {
                AddError(new OpportunityOfferPairNotBookableError(), "Opportunities that are cancelled are not bookable");
            }

            if (item.OrderedItem.Object.EventStatus == Schema.NET.EventStatusType.EventPostponed)
            {
                AddError(new OpportunityOfferPairNotBookableError(), "Opportunities that are postponed are not bookable");
            }

            if (item.AcceptedOffer.Object.ValidFromBeforeStartDate.HasValue && item.OrderedItem.Object.StartDate.HasValue
                && item.OrderedItem.Object.StartDate.Value - item.AcceptedOffer.Object.ValidFromBeforeStartDate.Value > DateTimeOffset.Now)
            {
                AddError(new OpportunityOfferPairNotBookableError(), "Opportunity is not yet within its booking window");
            }

            if (item.AcceptedOffer.Object.OpenBookingInAdvance == RequiredStatusType.Unavailable)
            {
                AddError(new OpportunityOfferPairNotBookableError(), "Open Booking is not available on this opportunity");
            }

            item.Error = Errors;
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
        private class SilentRollbackException : Exception {}

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

        protected override async Task<Event> InsertTestOpportunity(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, TestOpenBookingFlowEnumeration openBookingFlow, SimpleIdComponents seller)
        {
            if (!storeRouting.ContainsKey(opportunityType))
                throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "Specified opportunity type is not configured as bookable in the StoreBookingEngine constructor.");

            return await storeRouting[opportunityType].CreateOpportunityWithinTestDataset(testDatasetIdentifier, opportunityType, criteria, openBookingFlow, seller);
        }

        protected override async Task DeleteTestDataset(string testDatasetIdentifier)
        {
            foreach (var store in storeRouting.Values)
            {
                await store.DeleteTestDataset(testDatasetIdentifier);
            }
        }

        protected override async Task TriggerTestAction(OpenBookingSimulateAction simulateAction, OrderIdTemplate orderIdTemplate)
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


        public override async Task ProcessCustomerCancellation(OrderIdComponents orderId, SimpleIdComponents sellerId, SimpleIdComponents customerAccountIdComponents, OrderIdTemplate orderIdTemplate, List<OrderIdComponents> orderItemIds)
        {
            if (!await storeBookingEngineSettings.OrderStore.CustomerCancelOrderItems(orderId, sellerId, orderItemIds))
            {
                throw new OpenBookingException(new UnknownOrderError(), "Order not found");
            }
        }

        public override async Task ProcessOrderProposalCustomerRejection(OrderIdComponents orderId, SimpleIdComponents sellerId, SimpleIdComponents customerAccountIdComponents, OrderIdTemplate orderIdTemplate)
        {
            if (!await storeBookingEngineSettings.OrderStore.CustomerRejectOrderProposal(orderId, sellerId))
            {
                throw new OpenBookingException(new UnknownOrderError(), "OrderProposal not found");
            }
        }

        protected override async Task<DeleteOrderResult> ProcessOrderDeletion(OrderIdComponents orderId, SimpleIdComponents sellerId, SimpleIdComponents customerAccountIdComponents)
        {
            return await storeBookingEngineSettings.OrderStore.DeleteOrder(orderId, sellerId);
        }

        protected override async Task ProcessOrderQuoteDeletion(OrderIdComponents orderId, SimpleIdComponents sellerId, SimpleIdComponents customerAccountIdComponents)
        {
            await storeBookingEngineSettings.OrderStore.DeleteLease(orderId, sellerId);
        }

        private static void CheckOrderIntegrity(Order requestOrder, Order responseOrder)
        {
            // If any other errors were returned from GetOrderItems, the booking must fail
            // https://www.openactive.io/open-booking-api/EditorsDraft/#order-creation-b
            if (responseOrder.OrderedItem.Any(x => x.Error != null && x.Error.Count > 0))
            {
                throw new SilentRollbackException();
            }

            // Throw error on payment due mismatch
            if (requestOrder.TotalPaymentDue?.Price != responseOrder.TotalPaymentDue?.Price)
            {
                throw new OpenBookingException(new TotalPaymentDueMismatchError());
            }

            // If no payment provided by broker, prepayment must either be required, or not specified with a nonzero price
            if (requestOrder.Payment == null && (
                    responseOrder.TotalPaymentDue?.OpenBookingPrepayment == RequiredStatusType.Required ||
                    responseOrder.TotalPaymentDue?.Price > 0 && responseOrder.TotalPaymentDue?.OpenBookingPrepayment == null))
            {
                throw new OpenBookingException(new MissingPaymentDetailsError(), "Orders with prepayment must have nonzero price.");
            }

            // If payment provided by broker, prepayment must not be unavailable and price must not be zero
            if (requestOrder.Payment != null && (
                    responseOrder.TotalPaymentDue?.OpenBookingPrepayment == RequiredStatusType.Unavailable ||
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

        protected override async Task<Order> ProcessGetOrderStatus(OrderIdComponents orderId, SimpleIdComponents sellerIdComponents, ILegalEntity seller, SimpleIdComponents customerAccountIdComponents)
        {
            // Get Order without OrderItems expanded
            var order = await storeBookingEngineSettings.OrderStore.GetOrderStatus(orderId, sellerIdComponents, seller);

            // Get flowContext from resulting Order, treating it like a request (which also validates it like a request)
            var flowContext = AugmentContextFromOrder(ValidateFlowRequest<Order>(orderId, sellerIdComponents, seller, customerAccountIdComponents, FlowStage.OrderStatus, order), order);

            // Expand OrderItems based on the flowContext
            // Get contexts from OrderItems
            var orderItemContexts = GetOrderItemContexts(order.OrderedItem);

            // Add to flowContext for reference throughout the flow
            flowContext.OrderItemContexts = orderItemContexts;

            // Call GetOrderItems for each Store to expand the OrderItems
            await GetOrderItemContextGroups(orderItemContexts, flowContext, null);

            // Maintain IDs and OrderItemStatus from GetOrderStatus that will have been overwritten by expansion
            foreach (var ctx in orderItemContexts)
            {
                ctx.ResponseOrderItem.Id = ctx.RequestOrderItem.Id;
                ctx.ResponseOrderItem.OrderItemStatus = ctx.RequestOrderItem.OrderItemStatus;
            }
            order.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

            // Add totals to the resulting Order
            OrderCalculations.AugmentOrderWithTotals(
                order, flowContext, storeBookingEngineSettings.BusinessToConsumerTaxCalculation, storeBookingEngineSettings.BusinessToBusinessTaxCalculation, storeBookingEngineSettings.PrepaymentAlwaysRequired);

            // TODO: Should other properties be extracted from the flowContext for consistency, or do we trust the internals not to create excessive props?
            order.BookingService = flowContext.BookingService;
            return order;
        }

        public override async Task<Order> ProcessOrderCreationFromOrderProposal(OrderIdComponents orderId, OrderIdTemplate orderIdTemplate, ILegalEntity seller, SimpleIdComponents sellerId, SimpleIdComponents customerAccountIdComponents, Order order)
        {
            if (!await storeBookingEngineSettings.OrderStore.CreateOrderFromOrderProposal(orderId, sellerId, order.OrderProposalVersion, order))
            {
                throw new OpenBookingException(new OrderProposalVersionOutdatedError());
            }
            return await ProcessGetOrderStatus(orderId, sellerId, seller, customerAccountIdComponents);
        }

        private List<IOrderItemContext> GetOrderItemContexts(List<OrderItem> sourceOrderItems)
        {
            // Create OrderItemContext for each OrderItem
            return sourceOrderItems.Select((orderItem, index) =>
            {
                var orderedItemId = orderItem.OrderedItem.IdReference;
                var acceptedOfferId = orderItem.AcceptedOffer.IdReference;

                if (orderedItemId == null)
                {
                    return new UnknownOrderItemContext(index, orderItem,
                        new IncompleteOrderItemError(), "orderedItem @id was not provided");
                }

                /*
                TODO: Check if Customer Account auth and throw if not
                if (acceptedOfferId == null)
                {
                    return new UnknownOrderItemContext(index, orderItem,
                        new IncompleteOrderItemError(), "acceptedOffer @id was not provided");
                }
                */

                var idComponents = base.ResolveOpportunityID(orderedItemId, acceptedOfferId);

                if (idComponents == null)
                {
                    return new UnknownOrderItemContext(index, orderItem,
                        new InvalidOpportunityOrOfferIdError(), $"Opportunity @id and Offer @id pair are not in the expected format: '{orderedItemId}' and '{acceptedOfferId}'");
                }

                if (idComponents.OpportunityType == null)
                {
                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "OpportunityType must be configured for each IdComponent entry in the settings.");
                }

                // Create the relevant OrderItemContext using the specific type of the IdComponents returned
                Type type = typeof(OrderItemContext<>).MakeGenericType(idComponents.GetType());
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                IOrderItemContext orderItemContext = (IOrderItemContext)Activator.CreateInstance(
                    type, flags, null, new object[] { index, idComponents, orderItem }, CultureInfo.InvariantCulture);
                return orderItemContext;

            }).ToList();
        }

        private async Task<List<OrderItemContextGroup>> GetOrderItemContextGroups(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext)
        {
            // Group by OpportunityType for processing
            var orderItemGroupsTasks = orderItemContexts
                .Where(ctx => !ctx.IsSkeleton) // Exclude any items where SetResponseOrderItemAsSkeleton has been used
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
            orderItemGroups.AddRange(await Task.WhenAll(orderItemGroupsTasks).ConfigureAwait(true));

            // Add missing types and stores to ensure stores are always executed, even when there are no relevant items,
            // to ensure they have an opportunity to clean up from previous leases where there were relevant items
            var missingOrderItemGroups = storeRouting.Keys.Except(orderItemGroups.Select(x => x.OpportunityType));
            orderItemGroups.AddRange(missingOrderItemGroups.Select(opportunityType => new OrderItemContextGroup
            {
                OpportunityType = opportunityType,
                Store = storeRouting[opportunityType],
                OrderItemContexts = new List<IOrderItemContext>()
            }));

            return orderItemGroups;
        }

        private StoreBookingFlowContext AugmentContextFromOrder<TOrder>(BookingFlowContext request, TOrder order) where TOrder : Order, new()
        {
            StoreBookingFlowContext context = new StoreBookingFlowContext(request);

            // If this is a Customer Account request
            if (context.CustomerAccountId != null)
            {
                // TODO: use CustomerAccountStore to get Customer details
                // QUICK HACK
                context.Customer = new Person
                {
                    Email = "a-customer-account@test.com",
                    HasAccount = new CustomerAccount
                    {
                        Identifier = context.CustomerAccountId.IdGuid.ToString()
                    }
                };
            } else if (order.Customer.HasAccount.IdReference != null) {
                // TODO: REMOVE THIS, as this is not in the latest spec - the latest spec just relies on the auth token

                // QUICK HACK
                context.Customer = new Person
                {
                    Email = "a-customer-account@test.com",
                    HasAccount = new CustomerAccount
                    {
                        Id = order.Customer.HasAccount.IdReference
                    }
                };
                var template = new SingleIdTemplate<SimpleIdComponents>(
                    "{+BaseUrl}/api/customer-accounts/{IdGuid}"
                );
                // Hack to add CustomerAccountId
                context.CustomerAccountId = template.GetIdComponents(order.Customer.HasAccount.IdReference);
            }
            else
            {
                // Reflect back only those customer fields that are supported
                switch (order.Customer)
                {
                    case Person person:
                        context.Customer = storeBookingEngineSettings.CustomerPersonSupportedFields(person);
                        break;

                    case Organization organization:
                        context.Customer = storeBookingEngineSettings.CustomerOrganizationSupportedFields(organization);
                        break;
                }
            }

            // Throw error on incomplete broker details
            if (order.BrokerRole != BrokerType.NoBroker && (order.Broker == null || string.IsNullOrWhiteSpace(order.Broker.Name)))
            {
                throw new OpenBookingException(new IncompleteBrokerDetailsError());
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

            // Get static BookingService fields from settings
            context.BookingService = storeBookingEngineSettings.BookingServiceDetails;

            return context;
        }

        public void AugmentWithOpenBookingPrepaymentConflictErrors(List<IOrderItemContext> orderItemContexts) {
            var contextsWithOpenBookingPrepaymentRequired = orderItemContexts.Where(x => x.ResponseOrderItem?.AcceptedOffer.Object?.Price > 0 && (x.ResponseOrderItem?.AcceptedOffer.Object?.OpenBookingPrepayment == null || x.ResponseOrderItem?.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Required)).ToList();
            var contextsWithOpenBookingPrepaymentUnavailable = orderItemContexts.Where(x => x.ResponseOrderItem?.AcceptedOffer.Object?.Price > 0 && x.ResponseOrderItem?.AcceptedOffer.Object?.OpenBookingPrepayment == RequiredStatusType.Unavailable).ToList();

            // Add errors to any items with conflicting openBookingPrepayment values
            if (contextsWithOpenBookingPrepaymentRequired.Count > 0 && contextsWithOpenBookingPrepaymentUnavailable.Count > 0) {
                foreach (var ctx in contextsWithOpenBookingPrepaymentRequired.Concat(contextsWithOpenBookingPrepaymentUnavailable))
                {
                    ctx.AddError(new OpportunityIsInConflictError(), "A single Order cannot contain items with prepayment Unavailable, and also items with prepayment Required.");
                }
            }
        }

        public override async Task<TOrder> ProcessFlowRequest<TOrder>(BookingFlowContext request, TOrder order)
        {
            var flowContext = AugmentContextFromOrder(request, order);

            // Get contexts from OrderItems
            var orderItemContexts = GetOrderItemContexts(order.OrderedItem);

            // Add to flowContext for reference throughout the flow
            flowContext.OrderItemContexts = orderItemContexts;

            // StateContext is useful for transferring state between stages of the flow, and initialising disposable resources for use throughout the flow
            using (var stateContext = await storeBookingEngineSettings.OrderStore.CreateOrderStateContext(flowContext))
            {
                Console.WriteLine($"## {flowContext.OrderId.uuid} | ENTERING CRITICAL SECTION {flowContext.Stage.ToString()} for {flowContext?.Customer?.Email ?? "?"}");

                // Runs before the flow starts, for both leasing and booking
                await storeBookingEngineSettings.OrderStore.Initialise(flowContext, stateContext);

                // Call GetOrderItems for each Store
                var orderItemGroups = await GetOrderItemContextGroups(orderItemContexts, flowContext, stateContext);

                // Add errors to any items with conflicting openBookingPrepayment values
                AugmentWithOpenBookingPrepaymentConflictErrors(orderItemContexts);

                // Create a response Order based on the original order of the OrderItems in orderItemContexts
                TOrder responseGenericOrder = new TOrder
                {
                    Id = flowContext.OrderIdTemplate.RenderOrderId(flowContext.OrderId),
                    BrokerRole = flowContext.BrokerRole,
                    Broker = flowContext.Broker,
                    Seller = new ReferenceValue<ILegalEntity>(flowContext.Seller),
                    Customer = flowContext.Customer,
                    BookingService = flowContext.BookingService,
                    Payment = flowContext.Payment,
                    OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList()
                };

                // Add totals to the resulting Order
                OrderCalculations.AugmentOrderWithTotals(
                    responseGenericOrder, flowContext, storeBookingEngineSettings.BusinessToConsumerTaxCalculation, storeBookingEngineSettings.BusinessToBusinessTaxCalculation, storeBookingEngineSettings.PrepaymentAlwaysRequired);

                try {
                    switch (responseGenericOrder)
                    {
                        case OrderProposal responseOrderProposal:
                            if (flowContext.Stage != FlowStage.P)
                                throw new OpenBookingException(new UnexpectedOrderTypeError());

                            CheckOrderIntegrity(order, responseOrderProposal);

                            // Proposal creation is atomic
                            using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.EnforceSyncWithinOrderTransactions
                                ? storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage).CheckSyncValueTaskWorkedAndReturnResult()
                                : await storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage))
                            {
                                if (dbTransaction == null)
                                {
                                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "A transaction is required for OrderProposal Creation at P, to ensure the integrity of the booking made.");
                                }

                                try
                                {
                                    // Create the parent Order
                                    var (version, orderProposalStatus) = storeBookingEngineSettings.EnforceSyncWithinOrderTransactions ?
                                            storeBookingEngineSettings.OrderStore.CreateOrderProposal(responseOrderProposal, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorkedAndReturnResult()
                                            : await storeBookingEngineSettings.OrderStore.CreateOrderProposal(responseOrderProposal, flowContext, stateContext, dbTransaction);

                                    responseOrderProposal.OrderProposalVersion = new Uri($"{responseOrderProposal.Id}/versions/{version}");
                                    responseOrderProposal.OrderProposalStatus = orderProposalStatus;

                                    // Cleanup hook to allow cleanup of the OrderItems that are no longer included in this lease, but were added by a previous call
                                    // Note the happens first to ensure no conflicts with new OrderItems that have since been added
                                    foreach (var g in orderItemGroups)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            g.Store.CleanupOrderItems(null, g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                        else
                                            await g.Store.CleanupOrderItems(null, g.OrderItemContexts, flowContext, stateContext, dbTransaction);
                                    }

                                    // Book the OrderItems
                                    foreach (var g in orderItemGroups)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            g.Store.ProposeOrderItems(g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                        else
                                            await g.Store.ProposeOrderItems(g.OrderItemContexts, flowContext, stateContext, dbTransaction);


                                        foreach (var ctx in g.OrderItemContexts)
                                        {
                                            // Check that OrderItem Id was added
                                            if ((ctx.ResponseOrderItemId == null || ctx.ResponseOrderItem.Id == null) && !ctx.HasErrors)
                                            {
                                                throw new OpenBookingException(new InternalLibraryError(), "SetOrderItemId must be called for each OrderItemContext in ProposeOrderItems for successfully booked items");
                                            }

                                            // Set the orderItemStatus to null (as it must always be so in the response of P)
                                            ctx.ResponseOrderItem.OrderItemStatus = null;
                                        }
                                    }

                                    // Update this in case ResponseOrderItem was overwritten in ProposeOrderItems
                                    responseOrderProposal.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                                    // Recheck for integrity given the updates
                                    CheckOrderIntegrity(order, responseOrderProposal);

                                    if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                        storeBookingEngineSettings.OrderStore.UpdateOrderProposal(responseOrderProposal, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                    else
                                        await storeBookingEngineSettings.OrderStore.UpdateOrderProposal(responseOrderProposal, flowContext, stateContext, dbTransaction);

                                    if (dbTransaction != null)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            dbTransaction.Commit().CheckSyncValueTaskWorked();
                                        else
                                            await dbTransaction.Commit();
                                    }
                                }
                                catch
                                {
                                    if (dbTransaction != null)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            dbTransaction.Rollback().CheckSyncValueTaskWorked();
                                        else
                                            await dbTransaction.Rollback();
                                    }
                                    throw;
                                }
                            }
                            break;

                        case OrderQuote responseOrderQuote:
                            if (!(flowContext.Stage == FlowStage.C1 || flowContext.Stage == FlowStage.C2))
                                throw new OpenBookingException(new UnexpectedOrderTypeError());

                            // If "payment" has been supplied unnecessarily, simply do not return it
                            if (responseOrderQuote.Payment != null && responseOrderQuote.TotalPaymentDue.Price.Value == 0)
                            {
                                responseOrderQuote.Payment = null;
                            }

                            // Note behaviour here is to lease those items that are available to be leased, and return errors for everything else
                            // Leasing is optimistic, booking is atomic
                            using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.EnforceSyncWithinOrderTransactions
                                ? storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage).CheckSyncValueTaskWorkedAndReturnResult()
                                : await storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage))
                            {
                                try
                                {

                                    responseOrderQuote.Lease = storeBookingEngineSettings.EnforceSyncWithinOrderTransactions ?
                                        storeBookingEngineSettings.OrderStore.CreateLease(responseOrderQuote, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorkedAndReturnResult()
                                        : await storeBookingEngineSettings.OrderStore.CreateLease(responseOrderQuote, flowContext, stateContext, dbTransaction);

                                    // Cleanup hook to allow cleanup of the OrderItems that are no longer included in this lease, but were added by a previous call
                                    // Note the happens first to ensure no conflicts with new OrderItems that have since been added
                                    foreach (var g in orderItemGroups)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            g.Store.CleanupOrderItems(responseOrderQuote.Lease, g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                        else
                                            await g.Store.CleanupOrderItems(responseOrderQuote.Lease, g.OrderItemContexts, flowContext, stateContext, dbTransaction);
                                    }

                                    if (responseOrderQuote.Lease != null)
                                    {
                                        foreach (var g in orderItemGroups)
                                        {
                                            if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                                g.Store.LeaseOrderItems(responseOrderQuote.Lease, g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                            else
                                                await g.Store.LeaseOrderItems(responseOrderQuote.Lease, g.OrderItemContexts, flowContext, stateContext, dbTransaction);
                                        }
                                    }

                                    // Update this in case ResponseOrderItem was overwritten in Lease
                                    responseOrderQuote.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                                    // Note OrderRequiresApproval is only required during C1 and C2
                                    responseOrderQuote.OrderRequiresApproval = orderItemContexts.Any(x => x.RequiresApproval);

                                    if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                        storeBookingEngineSettings.OrderStore.UpdateLease(responseOrderQuote, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                    else
                                        await storeBookingEngineSettings.OrderStore.UpdateLease(responseOrderQuote, flowContext, stateContext, dbTransaction);


                                    if (dbTransaction != null)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            dbTransaction.Commit().CheckSyncValueTaskWorked();
                                        else
                                            await dbTransaction.Commit();
                                    }
                                }
                                catch
                                {
                                    if (dbTransaction != null)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            dbTransaction.Rollback().CheckSyncValueTaskWorked();
                                        else
                                            await dbTransaction.Rollback();
                                    }
                                    throw;
                                }
                            }
                            break;

                        case Order responseOrder:
                            if (flowContext.Stage != FlowStage.B)
                                throw new OpenBookingException(new UnexpectedOrderTypeError());

                            CheckOrderIntegrity(order, responseOrder);

                            // Booking is atomic
                            using (IDatabaseTransaction dbTransaction = storeBookingEngineSettings.EnforceSyncWithinOrderTransactions
                                ? storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage).CheckSyncValueTaskWorkedAndReturnResult()
                                : await storeBookingEngineSettings.OrderStore.BeginOrderTransaction(flowContext.Stage))
                            {
                                if (dbTransaction == null)
                                {
                                    throw new InternalOpenBookingException(new InternalLibraryConfigurationError(), "A transaction is required for booking at B, to ensure the integrity of the booking made.");
                                }

                                try
                                {
                                    // Create the parent Order
                                    if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                        storeBookingEngineSettings.OrderStore.CreateOrder(responseOrder, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                    else
                                        await storeBookingEngineSettings.OrderStore.CreateOrder(responseOrder, flowContext, stateContext, dbTransaction);

                                    // Cleanup hook to allow cleanup of the OrderItems that are no longer included in this lease, but were added by a previous call
                                    // Note the happens first to ensure no conflicts with new OrderItems that have since been added
                                    foreach (var g in orderItemGroups)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            g.Store.CleanupOrderItems(null, g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                        else
                                            await g.Store.CleanupOrderItems(null, g.OrderItemContexts, flowContext, stateContext, dbTransaction);
                                    }

                                    // Book the OrderItems
                                    foreach (var g in orderItemGroups)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            g.Store.BookOrderItems(g.OrderItemContexts, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                        else
                                            await g.Store.BookOrderItems(g.OrderItemContexts, flowContext, stateContext, dbTransaction);

                                        foreach (var ctx in g.OrderItemContexts)
                                        {
                                            // Check that OrderItem Id was added
                                            if ((ctx.ResponseOrderItemId == null || ctx.ResponseOrderItem.Id == null) && !ctx.HasErrors)
                                            {
                                                throw new OpenBookingException(new InternalLibraryError(), "SetOrderItemId must be called for each OrderItemContext in BookOrderItems for successfully booked items");
                                            }

                                            // Set the orderItemStatus to be https://openactive.io/OrderConfirmed (as it must always be so in the response of B)
                                            ctx.ResponseOrderItem.OrderItemStatus = OrderItemStatus.OrderItemConfirmed;
                                        }
                                    }

                                    // Update this in case ResponseOrderItem was overwritten in Book
                                    responseOrder.OrderedItem = orderItemContexts.Select(x => x.ResponseOrderItem).ToList();

                                    // Recheck integrity given the updates
                                    CheckOrderIntegrity(order, responseOrder);

                                    if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                        storeBookingEngineSettings.OrderStore.UpdateOrder(responseOrder, flowContext, stateContext, dbTransaction).CheckSyncValueTaskWorked();
                                    else
                                        await storeBookingEngineSettings.OrderStore.UpdateOrder(responseOrder, flowContext, stateContext, dbTransaction);


                                    if (dbTransaction != null)
                                    {
                                            if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                                dbTransaction.Commit().CheckSyncValueTaskWorked();
                                            else
                                                await dbTransaction.Commit();
                                    }
                                }
                                catch
                                {
                                    if (dbTransaction != null)
                                    {
                                        if (storeBookingEngineSettings.EnforceSyncWithinOrderTransactions)
                                            dbTransaction.Rollback().CheckSyncValueTaskWorked();
                                        else
                                            await dbTransaction.Rollback();
                                    }
                                    throw;
                                }
                            }
                            break;

                        default:
                            throw new OpenBookingException(new UnexpectedOrderTypeError());
                    }
                }
                catch (SilentRollbackException) {
                    // Catch the SilentRollbackException so it doesn't leave the method, but do nothing with it
                    // At this point it will have already triggered the rollback
                } finally
                {
                    Console.WriteLine($"## {flowContext.OrderId.uuid} | LEAVING CRITICAL SECTION {flowContext.Stage.ToString()} for {flowContext?.Customer?.Email ?? "?"}");
                }

                return responseGenericOrder;
            }
        }
    }

    internal class OrderItemContextGroup
    {
        public OpportunityType OpportunityType { get; set; }
        public IOpportunityStore Store { get; set; }
        public List<IOrderItemContext> OrderItemContexts { get; set; }
    }
}

