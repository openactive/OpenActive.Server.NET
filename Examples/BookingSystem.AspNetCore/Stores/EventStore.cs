using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenActive.DatasetSite.NET;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.Server.NET.StoreBooking;
using ServiceStack.OrmLite;
using RequiredStatusType = OpenActive.FakeDatabase.NET.RequiredStatusType;

namespace BookingSystem
{
    class EventStore : OpportunityStore<EventOpportunity, OrderTransaction, OrderStateContext>
    {
        private readonly AppSettings _appSettings;

        // Example constructor that can set state from EngineConfig. This is not required for an actual implementation.
        public EventStore(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        Random rnd = new Random();

        protected async override Task<EventOpportunity> CreateOpportunityWithinTestDataset(
            string testDatasetIdentifier,
            OpportunityType opportunityType,
            TestOpportunityCriteriaEnumeration criteria,
            TestOpenBookingFlowEnumeration openBookingFlow,
              SellerIdComponents seller)
        {
            if (!_appSettings.FeatureFlags.SingleSeller && !seller.SellerIdLong.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "Seller must have an ID in Multiple Seller Mode");

            long? sellerId = _appSettings.FeatureFlags.SingleSeller ? null : seller.SellerIdLong;
            var requiresApproval = openBookingFlow == TestOpenBookingFlowEnumeration.OpenBookingApprovalFlow;

            switch (opportunityType)
            {
                case OpportunityType.Event:
                    int eventId;
                    switch (criteria)
                    {
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityOfflineBookable:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event",
                                 rnd.Next(2) == 0 ? 0M : 14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFree:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableUsingPayment:
                            eventId = FakeBookingSystem.Database.AddEvent(
                             testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFree:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event",
                                0M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableOutsideValidFromBeforeStartDate:
                            {
                                var isValid = criteria == TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate;
                                eventId = FakeBookingSystem.Database.AddEvent(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event {(isValid ? "Within" : "Outside")} Window",
                                    14.99M,
                                    10,
                                    requiresApproval,
                                    validFromStartDate: isValid);
                            }
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableWithinWindow:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableOutsideWindow:
                            {
                                var isValid = criteria == TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableWithinWindow;
                                eventId = FakeBookingSystem.Database.AddEvent(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event {(isValid ? "Within" : "Outside")} Cancellation Window",
                                    14.99M,
                                    10,
                                    requiresApproval,
                                    latestCancellationBeforeStartDate: isValid);
                            }
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentOptional:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Optional",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Optional);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentUnavailable:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Unavailable",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Unavailable);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentRequired:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Required",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Required);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNoSpaces:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event No Spaces",
                                14.99M,
                                0,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFiveSpaces:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event Five Spaces",
                                14.99M,
                                5,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxNet:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                2,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Tax Net",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxGross:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Tax Gross",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableSellerTermsOfService:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event With Seller Terms Of Service",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAttendeeDetails:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event That Requires Attendee Details",
                                14.99M,
                                10,
                                requiresApproval,
                                requiresAttendeeValidation: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAdditionalDetails:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event That Requires Additional Details",
                                10M,
                                10,
                                requiresApproval,
                                requiresAdditionalDetails: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithNegotiation:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event That Allows Proposal Amendment",
                                10M,
                                10,
                                requiresApproval,
                                allowProposalAmendment: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNotCancellable:
                            eventId = FakeBookingSystem.Database.AddEvent(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event Paid That Does Not Allow Full Refund",
                                10M,
                                10,
                                requiresApproval,
                                allowCustomerCancellationFullRefund: false);
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "testOpportunityCriteria value not supported");
                    }

                    return new EventOpportunity
                    {
                        OpportunityType = opportunityType,
                        EventId = eventId
                    };
                default:
                    throw new OpenBookingException(new OpenBookingError(), "Opportunity Type not supported");
            }
        }

        protected async override Task DeleteTestDataset(string testDatasetIdentifier)
        {
            FakeBookingSystem.Database.DeleteTestEventsFromDataset(testDatasetIdentifier);
        }

        protected async override Task TriggerTestAction(OpenBookingSimulateAction simulateAction, EventOpportunity idComponents)
        {
            switch (simulateAction)
            {
                case ChangeOfLogisticsTimeSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateEventStartAndEndTimeByPeriodInMins(idComponents.EventId.Value, 60))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsNameSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateEventTitle(idComponents.EventId.Value, "Updated Event Title"))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsLocationSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateEventLocationLatLng(idComponents.EventId.Value, 0.2m, 0.3m))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }

        }

        // Similar to the RPDE logic, this needs to render and return an new hypothetical OrderItem from the database based on the supplied opportunity IDs
        protected async override Task GetOrderItems(List<OrderItemContext<EventOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {
            // Note the implementation of this method must also check that this OrderItem is from the Seller specified by context.SellerIdComponents (this is not required if using a Single Seller)

            // Additionally this method must check that there are enough spaces in each entry

            // Response OrderItems must be updated into supplied orderItemContexts (including duplicates for multi-party booking)

            var query = orderItemContexts.Select((orderItemContext) =>
            {
                var getOccurrenceInfoResult = FakeBookingSystem.Database.GetEventAndBookedOrderItemInfoByEventId(flowContext.OrderId.uuid, orderItemContext.RequestBookableOpportunityOfferId.EventId);
                var (hasFoundOccurrence, @event, bookedOrderItemInfo) = getOccurrenceInfoResult;
                if (hasFoundOccurrence == false)
                {
                    return null;
                }
                var remainingUsesIncludingOtherLeases = FakeBookingSystem.Database.GetNumberOfOtherLeasesForEvent(flowContext.OrderId.uuid, orderItemContext.RequestBookableOpportunityOfferId.EventId);

                return new
                {
                    OrderItem = new OrderItem
                    {
                        // TODO: The static example below should come from the database (which doesn't currently support tax)
                        UnitTaxSpecification = StoreHelper.GetUnitTaxSpecification(flowContext, _appSettings, @event.Price),
                        AcceptedOffer = new Offer
                        {
                            // Note this should always use RenderOfferId with the supplied SessionFacilityOpportunity, to take into account inheritance and OfferType
                            Id = RenderOfferId(orderItemContext.RequestBookableOpportunityOfferId),
                            Price = @event.Price,
                            PriceCurrency = "GBP",
                            LatestCancellationBeforeStartDate = @event.LatestCancellationBeforeStartDate,
                            OpenBookingPrepayment = @event.Prepayment.Convert(),
                            ValidFromBeforeStartDate = @event.ValidFromBeforeStartDate,
                            AllowCustomerCancellationFullRefund = @event.AllowCustomerCancellationFullRefund,
                        },
                        OrderedItem = new Event
                        {
                            // Note this should always be driven from the database, with new FacilityOpportunity's instantiated
                            Id = RenderOpportunityId(new EventOpportunity
                            {
                                OpportunityType = OpportunityType.Event,
                                EventId = @event.Id,

                            }),
                            Name = @event.Title,
                            Url = new Uri("https://example.com/events/" + @event.Id),
                            Location = new Place
                            {
                                Name = "Fake event",
                                Geo = new GeoCoordinates
                                {
                                    Latitude = @event.LocationLat,
                                    Longitude = @event.LocationLng,
                                }
                            },
                            Activity = new List<Concept>
                            {
                                new Concept
                                {
                                    Id = new Uri("https://openactive.io/activity-list#6bdea630-ad22-4e58-98a3-bca26ee3f1da"),
                                    PrefLabel = "Rave Fitness",
                                    InScheme = new Uri("https://openactive.io/activity-list")
                                }
                            },
                            StartDate = (DateTimeOffset)@event.Start,
                            EndDate = (DateTimeOffset)@event.End,
                            MaximumAttendeeCapacity = @event.TotalSpaces,
                            // Exclude current Order from the returned lease count
                            RemainingAttendeeCapacity = @event.RemainingSpaces - remainingUsesIncludingOtherLeases
                        },
                        Attendee = orderItemContext.RequestOrderItem.Attendee,
                        AttendeeDetailsRequired = @event.RequiresAttendeeValidation
                                        ? new List<PropertyEnumeration>
                                         {
                                             PropertyEnumeration.GivenName,
                                             PropertyEnumeration.FamilyName,
                                             PropertyEnumeration.Email,
                                             PropertyEnumeration.Telephone,
                                         }
                                        : null,
                        OrderItemIntakeForm = @event.RequiresAdditionalDetails
                                     ? PropertyValueSpecificationHelper.HydrateAdditionalDetailsIntoPropertyValueSpecifications(@event.RequiredAdditionalDetails)
                                     : null,
                        OrderItemIntakeFormResponse = orderItemContext.RequestOrderItem.OrderItemIntakeFormResponse,
                    },
                    SellerId = _appSettings.FeatureFlags.SingleSeller ? new SellerIdComponents() : new SellerIdComponents { SellerIdLong = @event.SellerId },
                    @event.RequiresApproval,
                    BookedOrderItemInfo = bookedOrderItemInfo,
                };
            });


            // Add the response OrderItems to the relevant contexts (note that the context must be updated within this method)
            foreach (var (item, ctx) in query.Zip(orderItemContexts, (item, ctx) => (item, ctx)))
            {
                if (item == null)
                {
                    ctx.SetResponseOrderItemAsSkeleton();
                    ctx.AddError(new UnknownOpportunityError());
                }
                else
                {
                    ctx.SetResponseOrderItem(item.OrderItem, item.SellerId, flowContext);

                    if (item.BookedOrderItemInfo != null)
                    {
                        BookedOrderItemHelper.AddPropertiesToBookedOrderItem(ctx, item.BookedOrderItemInfo);
                    }

                    if (item.RequiresApproval)
                        ctx.SetRequiresApproval();

                    if (item.OrderItem.OrderedItem.Object.RemainingAttendeeCapacity == 0)
                        ctx.AddError(new OpportunityIsFullError());

                    // Add validation errors to the OrderItem if either attendee details or additional details are required but not provided
                    var validationErrors = ctx.ValidateDetails(flowContext.Stage);
                    if (validationErrors.Count > 0)
                        ctx.AddErrors(validationErrors);
                }
            }
        }

        protected async override ValueTask LeaseOrderItems(
            Lease lease, List<OrderItemContext<EventOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.Event || !ctxGroup.Key.EventId.HasValue)
                {
                    foreach (var ctx in ctxGroup)
                    {
                        ctx.AddError(new OpportunityIntractableError(), "Opportunity ID and type are as not expected for the store. Likely a configuration issue with the Booking System.");
                    }
                }
                else
                {
                    // Attempt to lease for those with the same IDs, which is atomic
                    var (result, capacityErrors, capacityLeaseErrors) = FakeDatabase.LeaseOrderItemsForEvent(
                        databaseTransaction.FakeDatabaseTransaction,
                        flowContext.OrderId.ClientId,
                        flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                        flowContext.OrderId.uuid,
                        ctxGroup.Key.EventId.Value,
                        ctxGroup.Count());

                    switch (result)
                    {
                        case ReserveOrderItemsResult.Success:
                            // Do nothing, no errors to add
                            break;
                        case ReserveOrderItemsResult.SellerIdMismatch:
                            foreach (var ctx in ctxGroup)
                            {
                                ctx.AddError(new SellerMismatchError(), "An OrderItem SellerID did not match");
                            }
                            break;
                        case ReserveOrderItemsResult.OpportunityNotFound:
                            foreach (var ctx in ctxGroup)
                            {
                                ctx.AddError(new UnableToProcessOrderItemError(), "Opportunity not found");
                            }
                            break;
                        case ReserveOrderItemsResult.OpportunityOfferPairNotBookable:
                            foreach (var ctx in ctxGroup)
                            {
                                ctx.AddError(new OpportunityOfferPairNotBookableError(), "Opportunity not bookable");
                            }
                            break;
                        case ReserveOrderItemsResult.NotEnoughCapacity:
                            var contexts = ctxGroup.ToArray();
                            for (var i = contexts.Length - 1; i >= 0; i--)
                            {
                                var ctx = contexts[i];
                                if (capacityErrors > 0)
                                {
                                    ctx.AddError(new OpportunityHasInsufficientCapacityError());
                                    capacityErrors--;
                                }
                                else if (capacityLeaseErrors > 0)
                                {
                                    ctx.AddError(new OpportunityCapacityIsReservedByLeaseError());
                                    capacityLeaseErrors--;
                                }
                            }

                            break;
                        default:
                            foreach (var ctx in ctxGroup)
                            {
                                ctx.AddError(new OpportunityIntractableError(), "OrderItem could not be leased for unexpected reasons.");
                            }
                            break;
                    }
                }
            }
        }

        //TODO: This should reuse code of LeaseOrderItem
        protected async override ValueTask BookOrderItems(List<OrderItemContext<EventOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity
            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.Event || !ctxGroup.Key.EventId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the EventStore, during booking");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, bookedOrderItemInfos) = FakeDatabase.BookOrderItemsForEvent(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.EventId.Value,
                    RenderOpportunityId(ctxGroup.Key),
                    RenderOfferId(ctxGroup.Key),
                    ctxGroup.Count(),
                    false
                    );

                switch (result)
                {
                    case ReserveOrderItemsResult.Success:
                        foreach (var (ctx, bookedOrderItemInfo) in ctxGroup.Zip(bookedOrderItemInfos, (ctx, bookedOrderItemInfo) => (ctx, bookedOrderItemInfo)))
                        {
                            // Set OrderItemId and access properties for each orderItemContext
                            ctx.SetOrderItemId(flowContext, bookedOrderItemInfo.OrderItemId);
                            BookedOrderItemHelper.AddPropertiesToBookedOrderItem(ctx, bookedOrderItemInfo);
                        }
                        break;
                    case ReserveOrderItemsResult.SellerIdMismatch:
                        throw new OpenBookingException(new SellerMismatchError(), "An OrderItem SellerID did not match");
                    case ReserveOrderItemsResult.OpportunityNotFound:
                        throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity not found");
                    case ReserveOrderItemsResult.NotEnoughCapacity:
                        throw new OpenBookingException(new OpportunityHasInsufficientCapacityError());
                    case ReserveOrderItemsResult.OpportunityOfferPairNotBookable:
                        throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity and offer pair were not bookable");
                    default:
                        throw new OpenBookingException(new OrderCreationFailedError(), "Booking failed for an unexpected reason");
                }
            }
        }

        // TODO check logic here, it's just been copied from BookOrderItems. Possibly could remove duplication here.
        protected async override ValueTask ProposeOrderItems(List<OrderItemContext<EventOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.Event || !ctxGroup.Key.EventId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the EventStore, during proposal");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, bookedOrderItemInfos) = FakeDatabase.BookOrderItemsForEvent(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.EventId.Value,
                    RenderOpportunityId(ctxGroup.Key),
                    RenderOfferId(ctxGroup.Key),
                    ctxGroup.Count(),
                    true
                    );

                switch (result)
                {
                    case ReserveOrderItemsResult.Success:
                        foreach (var (ctx, bookedOrderItemInfo) in ctxGroup.Zip(bookedOrderItemInfos, (ctx, bookedOrderItemInfo) => (ctx, bookedOrderItemInfo)))
                        {
                            ctx.SetOrderItemId(flowContext, bookedOrderItemInfo.OrderItemId);
                        }
                        break;
                    case ReserveOrderItemsResult.SellerIdMismatch:
                        throw new OpenBookingException(new SellerMismatchError(), "An OrderItem SellerID did not match");
                    case ReserveOrderItemsResult.OpportunityNotFound:
                        throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity not found");
                    case ReserveOrderItemsResult.NotEnoughCapacity:
                        throw new OpenBookingException(new OpportunityHasInsufficientCapacityError());
                    case ReserveOrderItemsResult.OpportunityOfferPairNotBookable:
                        throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity and offer pair were not bookable");
                    default:
                        throw new OpenBookingException(new OrderCreationFailedError(), "Booking failed for an unexpected reason");
                }
            }
        }


    }



}
