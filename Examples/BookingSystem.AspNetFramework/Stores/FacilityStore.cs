﻿using System;
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
    class FacilityStore : OpportunityStore<FacilityOpportunity, OrderTransaction, OrderStateContext>
    {
        private readonly AppSettings _appSettings;

        // Example constructor that can set state from EngineConfig. This is not required for an actual implementation.
        public FacilityStore(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        Random rnd = new Random();

        protected async override Task<FacilityOpportunity> CreateOpportunityWithinTestDataset(
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
                case OpportunityType.FacilityUseSlot:
                    int facilityId, slotId;
                    switch (criteria)
                    {
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityOfflineBookable:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Facility",
                                 rnd.Next(2) == 0 ? 0M : 14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFree:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableUsingPayment:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                             testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFree:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Facility",
                                0M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableOutsideValidFromBeforeStartDate:
                            {
                                var isValid = criteria == TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate;
                                (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility {(isValid ? "Within" : "Outside")} Window",
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
                                (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility {(isValid ? "Within" : "Outside")} Cancellation Window",
                                    14.99M,
                                    10,
                                    requiresApproval,
                                    latestCancellationBeforeStartDate: isValid);
                            }
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentOptional:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility Prepayment Optional",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Optional);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentUnavailable:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility Prepayment Unavailable",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Unavailable);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentRequired:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility Prepayment Required",
                                14.99M,
                                10,
                                requiresApproval,
                                prepayment: RequiredStatusType.Required);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNoSpaces:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Facility No Spaces",
                                14.99M,
                                0,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFiveSpaces:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Facility Five Spaces",
                                14.99M,
                                5,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxNet:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                2,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility Tax Net",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxGross:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility Tax Gross",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableSellerTermsOfService:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Facility With Seller Terms Of Service",
                                14.99M,
                                10,
                                requiresApproval);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAttendeeDetails:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Facility That Requires Attendee Details",
                                14.99M,
                                10,
                                requiresApproval,
                                requiresAttendeeValidation: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAdditionalDetails:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility That Requires Additional Details",
                                10M,
                                10,
                                requiresApproval,
                                requiresAdditionalDetails: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithNegotiation:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Facility That Allows Proposal Amendment",
                                10M,
                                10,
                                requiresApproval,
                                allowProposalAmendment: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNotCancellable:
                            (facilityId, slotId) = FakeBookingSystem.Database.AddFacility(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Facility Paid That Does Not Allow Full Refund",
                                10M,
                                10,
                                requiresApproval,
                                allowCustomerCancellationFullRefund: false);
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "testOpportunityCriteria value not supported");
                    }

                    return new FacilityOpportunity
                    {
                        OpportunityType = opportunityType,
                        FacilityUseId = facilityId,
                        SlotId = slotId
                    };
                default:
                    throw new OpenBookingException(new OpenBookingError(), "Opportunity Type not supported");
            }
        }

        protected async override Task DeleteTestDataset(string testDatasetIdentifier)
        {
            FakeBookingSystem.Database.DeleteTestFacilitiesFromDataset(testDatasetIdentifier);
        }

        protected async override Task TriggerTestAction(OpenBookingSimulateAction simulateAction, FacilityOpportunity idComponents)
        {
            switch (simulateAction)
            {
                case ChangeOfLogisticsTimeSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateFacilitySlotStartAndEndTimeByPeriodInMins(idComponents.SlotId.Value, 60))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsNameSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateFacilityUseName(idComponents.SlotId.Value, "Updated Facility Title"))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsLocationSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateFacilityUseLocationLatLng(idComponents.SlotId.Value, 0.2m, 0.3m))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }

        }

        // Similar to the RPDE logic, this needs to render and return an new hypothetical OrderItem from the database based on the supplied opportunity IDs
        protected async override Task GetOrderItems(List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {
            // Note the implementation of this method must also check that this OrderItem is from the Seller specified by context.SellerIdComponents (this is not required if using a Single Seller)

            // Additionally this method must check that there are enough spaces in each entry

            // Response OrderItems must be updated into supplied orderItemContexts (including duplicates for multi-party booking)

            var query = orderItemContexts.Select((orderItemContext) =>
            {
                var getOccurrenceInfoResult = FakeBookingSystem.Database.GetSlotAndBookedOrderItemInfoBySlotId(flowContext.OrderId.uuid, orderItemContext.RequestBookableOpportunityOfferId.SlotId);
                var (hasFoundOccurrence, facility, slot, bookedOrderItemInfo) = getOccurrenceInfoResult;
                if (hasFoundOccurrence == false)
                {
                    return null;
                }
                var remainingUsesIncludingOtherLeases = FakeBookingSystem.Database.GetNumberOfOtherLeasesForSlot(flowContext.OrderId.uuid, orderItemContext.RequestBookableOpportunityOfferId.SlotId);

                return new
                {
                    OrderItem = new OrderItem
                    {
                        // TODO: The static example below should come from the database (which doesn't currently support tax)
                        UnitTaxSpecification = GetUnitTaxSpecification(flowContext, slot),
                        AcceptedOffer = new Offer
                        {
                            // Note this should always use RenderOfferId with the supplied SessionFacilityOpportunity, to take into account inheritance and OfferType
                            Id = RenderOfferId(orderItemContext.RequestBookableOpportunityOfferId),
                            Price = slot.Price,
                            PriceCurrency = "GBP",
                            LatestCancellationBeforeStartDate = slot.LatestCancellationBeforeStartDate,
                            OpenBookingPrepayment = slot.Prepayment.Convert(),
                            ValidFromBeforeStartDate = slot.ValidFromBeforeStartDate,
                            AllowCustomerCancellationFullRefund = slot.AllowCustomerCancellationFullRefund,
                        },
                        OrderedItem = new Slot
                        {
                            // Note this should always be driven from the database, with new FacilityOpportunity's instantiated
                            Id = RenderOpportunityId(new FacilityOpportunity
                            {
                                OpportunityType = OpportunityType.FacilityUseSlot,
                                FacilityUseId = slot.FacilityUseId,
                                SlotId = slot.Id
                            }),
                            FacilityUse = new FacilityUse
                            {
                                Id = RenderOpportunityId(new FacilityOpportunity
                                {
                                    OpportunityType = OpportunityType.FacilityUse,
                                    FacilityUseId = slot.FacilityUseId
                                }),
                                Name = facility.Name,
                                Url = new Uri("https://example.com/events/" + slot.FacilityUseId),
                                Location = new Place
                                {
                                    Name = "Fake fitness studio",
                                    Geo = new GeoCoordinates
                                    {
                                        Latitude = facility.LocationLat,
                                        Longitude = facility.LocationLng,
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
                            },
                            StartDate = (DateTimeOffset)slot.Start,
                            EndDate = (DateTimeOffset)slot.End,
                            MaximumUses = slot.MaximumUses,
                            // Exclude current Order from the returned lease count
                            RemainingUses = slot.RemainingUses - remainingUsesIncludingOtherLeases
                        },
                        Attendee = orderItemContext.RequestOrderItem.Attendee,
                        AttendeeDetailsRequired = slot.RequiresAttendeeValidation
                                        ? new List<PropertyEnumeration>
                                         {
                                             PropertyEnumeration.GivenName,
                                             PropertyEnumeration.FamilyName,
                                             PropertyEnumeration.Email,
                                             PropertyEnumeration.Telephone,
                                         }
                                        : null,
                        OrderItemIntakeForm = slot.RequiresAdditionalDetails
                                     ? PropertyValueSpecificationHelper.HydrateAdditionalDetailsIntoPropertyValueSpecifications(slot.RequiredAdditionalDetails)
                                     : null,
                        OrderItemIntakeFormResponse = orderItemContext.RequestOrderItem.OrderItemIntakeFormResponse,
                    },
                    SellerId = _appSettings.FeatureFlags.SingleSeller ? new SellerIdComponents() : new SellerIdComponents { SellerIdLong = facility.SellerId },
                    slot.RequiresApproval,
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

                    if (((Slot)item.OrderItem.OrderedItem.Object).RemainingUses == 0)
                        ctx.AddError(new OpportunityIsFullError());

                    // Add validation errors to the OrderItem if either attendee details or additional details are required but not provided
                    var validationErrors = ctx.ValidateDetails(flowContext.Stage);
                    if (validationErrors.Count > 0)
                        ctx.AddErrors(validationErrors);
                }
            }
        }

        protected async override ValueTask LeaseOrderItems(Lease lease, List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.Where(ctx => !ctx.HasErrors).GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.FacilityUseSlot || !ctxGroup.Key.SlotId.HasValue)
                {
                    foreach (var ctx in ctxGroup)
                    {
                        ctx.AddError(new OpportunityIntractableError(), "Opportunity ID and type are as not expected for the store. Likely a configuration issue with the Booking System.");
                    }
                }
                else
                {
                    // Attempt to lease for those with the same IDs, which is atomic
                    var (result, capacityErrors, capacityLeaseErrors) = FakeDatabase.LeaseOrderItemsForFacilitySlot(databaseTransaction.FakeDatabaseTransaction, flowContext.OrderId.ClientId, flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */, flowContext.OrderId.uuid, ctxGroup.Key.SlotId.Value, ctxGroup.Count());

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
        protected async override ValueTask BookOrderItems(List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity
            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.FacilityUseSlot || !ctxGroup.Key.SlotId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the FacilityStore, during booking");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, bookedOrderItemInfos) = FakeDatabase.BookOrderItemsForFacilitySlot(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.SlotId.Value,
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
        protected async override ValueTask ProposeOrderItems(List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.FacilityUseSlot || !ctxGroup.Key.SlotId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the FacilityStore, during proposal");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, bookedOrderItemInfos) = FakeDatabase.BookOrderItemsForFacilitySlot(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.SlotId.Value,
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

        private List<TaxChargeSpecification> GetUnitTaxSpecification(BookingFlowContext flowContext, SlotTable slot)
        {
            switch (flowContext.TaxPayeeRelationship)
            {
                case TaxPayeeRelationship.BusinessToBusiness when _appSettings.Payment.TaxCalculationB2B:
                case TaxPayeeRelationship.BusinessToConsumer when _appSettings.Payment.TaxCalculationB2C:
                    return new List<TaxChargeSpecification>
                    {
                        new TaxChargeSpecification
                        {
                            Name = "VAT at 20%",
                            Price = slot.Price * (decimal?) 0.2,
                            PriceCurrency = "GBP",
                            Rate = (decimal?) 0.2
                        }
                    };
                case TaxPayeeRelationship.BusinessToBusiness when !_appSettings.Payment.TaxCalculationB2B:
                case TaxPayeeRelationship.BusinessToConsumer when !_appSettings.Payment.TaxCalculationB2C:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
