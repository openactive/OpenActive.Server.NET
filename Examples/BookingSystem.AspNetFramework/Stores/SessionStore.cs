using OpenActive.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenActive.DatasetSite.NET;
using OpenActive.Server.NET.StoreBooking;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.FakeDatabase.NET;
using ServiceStack.OrmLite;
using RequiredStatusType = OpenActive.FakeDatabase.NET.RequiredStatusType;
using System.Threading.Tasks;

namespace BookingSystem
{
    class SessionStore : OpportunityStore<SessionOpportunity, OrderTransaction, OrderStateContext>
    {
        private readonly AppSettings _appSettings;

        // Example constructor that can set state from EngineConfig. This is not required for an actual implementation.
        public SessionStore(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        Random rnd = new Random();

        protected override SessionOpportunity CreateOpportunityWithinTestDataset(
            string testDatasetIdentifier,
            OpportunityType opportunityType,
            TestOpportunityCriteriaEnumeration criteria,
            SellerIdComponents seller)
        {
            if (!_appSettings.FeatureFlags.SingleSeller && !seller.SellerIdLong.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "Seller must have an ID in Multiple Seller Mode");

            long? sellerId = _appSettings.FeatureFlags.SingleSeller ? null : seller.SellerIdLong;

            switch (opportunityType)
            {
                case OpportunityType.ScheduledSession:
                    int classId, occurrenceId;
                    switch (criteria)
                    {
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookable:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event",
                                rnd.Next(2) == 0 ? 0M : 14.99M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFree:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableUsingPayment:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event",
                                14.99M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableOutsideValidFromBeforeStartDate:
                            {
                                var isValid = criteria == TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate;
                                (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event {(isValid ? "Within" : "Outside")} Window",
                                    14.99M,
                                    10,
                                    validFromStartDate: isValid);
                            }
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableWithinWindow:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableOutsideWindow:
                            {
                                var isValid = criteria == TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellableWithinWindow;
                                (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                    testDatasetIdentifier,
                                    sellerId,
                                    $"[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event {(isValid ? "Within" : "Outside")} Cancellation Window",
                                    14.99M,
                                    10,
                                    latestCancellationBeforeStartDate: isValid);
                            }
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentOptional:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Optional",
                                10M,
                                10,
                                prepayment: RequiredStatusType.Optional);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentUnavailable:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Unavailable",
                                10M,
                                10,
                                prepayment: RequiredStatusType.Unavailable);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreePrepaymentRequired:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Prepayment Required",
                                10M,
                                10,
                                prepayment: RequiredStatusType.Required);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFree:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event",
                                0M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNoSpaces:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event No Spaces",
                                14.99M,
                                0);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFiveSpaces:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event Five Spaces",
                                14.99M,
                                5);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFlowRequirementOnlyApproval:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event With Approval",
                                14.99M,
                                10,
                                requiresApproval: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxNet:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                2,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Tax Net",
                                14.99M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNonFreeTaxGross:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Tax Gross",
                                14.99M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableSellerTermsOfService:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                1,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event With Seller Terms Of Service",
                                14.99M,
                                10);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAttendeeDetails:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event That Requires Attendee Details",
                                10M,
                                10,
                                requiresAttendeeValidation: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableAdditionalDetails:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event That Requires Additional Details",
                                10M,
                                10,
                                requiresAdditionalDetails: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityOnlineBookable:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Virtual Event",
                                10M,
                                10,
                                isOnlineOrMixedAttendanceMode: true);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityOfflineBookable:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Offline Event",
                                10M,
                                10,
                                isOnlineOrMixedAttendanceMode: false);
                            break;
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNotCancellable:
                            (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                sellerId,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid That Does Not Allow Full Refund",
                                10M,
                                10,
                                allowCustomerCancellationFullRefund: false);
                            break;
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "testOpportunityCriteria value not supported");
                    }

                    return new SessionOpportunity
                    {
                        OpportunityType = opportunityType,
                        SessionSeriesId = classId,
                        ScheduledSessionId = occurrenceId
                    };

                default:
                    throw new OpenBookingException(new OpenBookingError(), "Opportunity Type not supported");
            }
        }

        protected override void DeleteTestDataset(string testDatasetIdentifier)
        {
            FakeBookingSystem.Database.DeleteTestClassesFromDataset(testDatasetIdentifier);
        }

        protected override void TriggerTestAction(OpenBookingSimulateAction simulateAction, SessionOpportunity idComponents)
        {
            switch (simulateAction)
            {
                case ChangeOfLogisticsTimeSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateScheduledSessionStartAndEndTimeByPeriodInMins(idComponents.ScheduledSessionId.Value, 60))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsNameSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateClassTitle(idComponents.ScheduledSessionId.Value, "Updated Class Title"))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                case ChangeOfLogisticsLocationSimulateAction _:
                    if (!FakeBookingSystem.Database.UpdateSessionSeriesLocationLatLng(idComponents.ScheduledSessionId.Value, 0.2m, 0.3m))
                    {
                        throw new OpenBookingException(new UnknownOpportunityError());
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }

        }

        protected async override Task GetOrderItems(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {
            GetOrderItemsSync(orderItemContexts, flowContext, stateContext);
        }

        // Similar to the RPDE logic, this needs to render and return an new hypothetical OrderItem from the database based on the supplied opportunity IDs
        private void GetOrderItemsSync(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {
            // Note the implementation of this method must also check that this OrderItem is from the Seller specified by context.SellerIdComponents (this is not required if using a Single Seller)

            // Additionally this method must check that there are enough spaces in each entry

            // Response OrderItems must be updated into supplied orderItemContexts (including duplicates for multi-party booking)

            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var occurrenceTable = db.Select<OccurrenceTable>();
                var classTable = db.Select<ClassTable>();

                var query = (from orderItemContext in orderItemContexts
                             join occurrences in occurrenceTable on orderItemContext.RequestBookableOpportunityOfferId.ScheduledSessionId equals occurrences.Id
                             join classes in classTable on occurrences.ClassId equals classes.Id
                             // and offers.id = opportunityOfferId.OfferId
                             select occurrences == null ? null : new
                             {
                                 OrderItem = new OrderItem
                                 {
                                     // TODO: The static example below should come from the database (which doesn't currently support tax)
                                     UnitTaxSpecification = GetUnitTaxSpecification(flowContext, classes),
                                     AcceptedOffer = new Offer
                                     {
                                         // Note this should always use RenderOfferId with the supplied SessionOpportunity, to take into account inheritance and OfferType
                                         Id = RenderOfferId(orderItemContext.RequestBookableOpportunityOfferId),
                                         Price = classes.Price,
                                         PriceCurrency = "GBP",
                                         LatestCancellationBeforeStartDate = classes.LatestCancellationBeforeStartDate,
                                         Prepayment = classes.Prepayment.Convert(),
                                         ValidFromBeforeStartDate = classes.ValidFromBeforeStartDate,
                                         AllowCustomerCancellationFullRefund = classes.AllowCustomerCancellationFullRefund,
                                     },
                                     OrderedItem = new ScheduledSession
                                     {
                                         // Note this should always be driven from the database, with new SessionOpportunity's instantiated
                                         Id = RenderOpportunityId(new SessionOpportunity
                                         {
                                             OpportunityType = OpportunityType.ScheduledSession,
                                             SessionSeriesId = occurrences.ClassId,
                                             ScheduledSessionId = occurrences.Id
                                         }),
                                         SuperEvent = new SessionSeries
                                         {
                                             Id = RenderOpportunityId(new SessionOpportunity
                                             {
                                                 OpportunityType = OpportunityType.SessionSeries,
                                                 SessionSeriesId = occurrences.ClassId
                                             }),
                                             Name = classes.Title,
                                             Url = new Uri("https://example.com/events/" + occurrences.ClassId),
                                             Location = new Place
                                             {
                                                 Name = "Fake fitness studio",
                                                 Geo = new GeoCoordinates
                                                 {
                                                     Latitude = 51.6201M,
                                                     Longitude = 0.302396M
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
                                             }
                                         },
                                         StartDate = (DateTimeOffset)occurrences.Start,
                                         EndDate = (DateTimeOffset)occurrences.End,
                                         MaximumAttendeeCapacity = occurrences.TotalSpaces,
                                         // Exclude current Order from the returned lease count
                                         RemainingAttendeeCapacity = occurrences.RemainingSpaces - db.Count<OrderItemsTable>(
                                            x => x.OrderTable.OrderMode != OrderMode.Booking &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.CustomerRejected &&
                                                 x.OrderTable.ProposalStatus != ProposalStatus.SellerRejected &&
                                                 x.OccurrenceId == occurrences.Id &&
                                                 x.OrderId != flowContext.OrderId.uuid)
                                     },
                                     Attendee = orderItemContext.RequestOrderItem.Attendee,
                                     AttendeeDetailsRequired = classes.RequiresAttendeeValidation
                                         ? new List<PropertyEnumeration>
                                         {
                                             PropertyEnumeration.GivenName,
                                             PropertyEnumeration.FamilyName,
                                             PropertyEnumeration.Email,
                                             PropertyEnumeration.Telephone,
                                         }
                                         : null,
                                     OrderItemIntakeForm = classes.RequiresAdditionalDetails
                                     ? PropertyValueSpecificationHelper.HydrateAdditionalDetailsIntoPropertyValueSpecifications(classes.RequiredAdditionalDetails)
                                     : null,
                                     OrderItemIntakeFormResponse = orderItemContext.RequestOrderItem.OrderItemIntakeFormResponse
                                 },
                                 SellerId = _appSettings.FeatureFlags.SingleSeller ? new SellerIdComponents() : new SellerIdComponents { SellerIdLong = classes.SellerId },
                                 classes.RequiresApproval
                             }).ToArray();

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
        }

        protected void LeaseOrderItemsSync(Lease lease, List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.ScheduledSession || !ctxGroup.Key.ScheduledSessionId.HasValue)
                {
                    foreach (var ctx in ctxGroup)
                    {
                        ctx.AddError(new OpportunityIntractableError(), "Opportunity ID and type are as not expected for the store. Likely a configuration issue with the Booking System.");
                    }
                }
                else
                {
                    // Attempt to lease for those with the same IDs, which is atomic
                    var (result, capacityErrors, capacityLeaseErrors) = FakeDatabase.LeaseOrderItemsForClassOccurrence(
                        databaseTransaction.FakeDatabaseTransaction,
                        flowContext.OrderId.ClientId,
                        flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                        flowContext.OrderId.uuid,
                        ctxGroup.Key.ScheduledSessionId.Value,
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
        protected void BookOrderItemsSync(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.ScheduledSession || !ctxGroup.Key.ScheduledSessionId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the SessionStore, during booking");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, bookedOrderItemInfos) = FakeDatabase.BookOrderItemsForClassOccurrence(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.ScheduledSessionId.Value,
                    RenderOpportunityJsonLdType(ctxGroup.Key),
                    RenderOpportunityId(ctxGroup.Key).ToString(),
                    RenderOfferId(ctxGroup.Key).ToString(),
                    ctxGroup.Count(),
                    false
                    );

                switch (result)
                {
                    case ReserveOrderItemsResult.Success:
                        // Set OrderItemId for each orderItemContext
                        foreach (var (ctx, bookedOrderItemInfo) in ctxGroup.Zip(bookedOrderItemInfos, (ctx, bookedOrderItemInfo) => (ctx, bookedOrderItemInfo)))
                        {
                            ctx.SetOrderItemId(flowContext, bookedOrderItemInfo.OrderItemId);
                            // Setting the access code and access pass after booking.
                            // If online session, add accessChannel
                            if (bookedOrderItemInfo.AttendanceMode == AttendanceMode.Online || bookedOrderItemInfo.AttendanceMode == AttendanceMode.Mixed)
                            {
                                ctx.ResponseOrderItem.AccessChannel = new VirtualLocation()
                                {
                                    Name = "Zoom Video Chat",
                                    Url = bookedOrderItemInfo.MeetingUrl,
                                    AccessId = bookedOrderItemInfo.MeetingId,
                                    AccessCode = bookedOrderItemInfo.MeetingPassword,
                                    Description = "Please log into Zoom a few minutes before the event"
                                };
                            }

                            // If offline session, add accessCode and accessPass
                            if (bookedOrderItemInfo.AttendanceMode == AttendanceMode.Offline || bookedOrderItemInfo.AttendanceMode == AttendanceMode.Mixed)
                            {
                                ctx.ResponseOrderItem.AccessCode = new List<PropertyValue>
                                {
                                    new PropertyValue()
                                    {
                                        Name = "Pin Code",
                                        Description = bookedOrderItemInfo.PinCode,
                                        Value = "defaultValue"
                                    }
                                };
                                ctx.ResponseOrderItem.AccessPass = new List<ImageObject>
                                {
                                    new ImageObject()
                                    {
                                        Url = new Uri(bookedOrderItemInfo.ImageUrl)
                                    },
                                    new Barcode()
                                    {
                                        Url = new Uri(bookedOrderItemInfo.ImageUrl),
                                        Text = bookedOrderItemInfo.BarCodeText,
                                        CodeType = "code128"
                                    }
                                };
                            }
                            // The request OrderItem can include an AccessPass if it is a Broker provided access pass
                            // In OrderItem, accessPass is an Image[], so needs to be cast to Barcode where applicable
                            var requestBarcodes = ctx.RequestOrderItem.AccessPass?.OfType<Barcode>().ToList();
                            if (requestBarcodes?.Count > 0)
                            {
                                if (ctx.ResponseOrderItem.AccessPass == null)
                                {
                                    ctx.ResponseOrderItem.AccessPass = new List<ImageObject>();

                                }
                                ctx.ResponseOrderItem.AccessPass.AddRange(requestBarcodes);
                            }
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
        protected void ProposeOrderItemsSync(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.ScheduledSession || !ctxGroup.Key.ScheduledSessionId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity ID and type are as not expected for the SessionStore, during proposal");
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, _) = FakeDatabase.BookOrderItemsForClassOccurrence(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.ScheduledSessionId.Value,
                    RenderOpportunityJsonLdType(ctxGroup.Key),
                    RenderOpportunityId(ctxGroup.Key).ToString(),
                    RenderOfferId(ctxGroup.Key).ToString(),
                    ctxGroup.Count(),
                    true
                    );

                switch (result)
                {
                    case ReserveOrderItemsResult.Success:
                        // Do nothing
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

        private List<TaxChargeSpecification> GetUnitTaxSpecification(BookingFlowContext flowContext, ClassTable classes)
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
                            Price = classes.Price * (decimal?)0.2,
                            PriceCurrency = "GBP",
                            Rate = (decimal?)0.2
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

    class SessionStoreSync : SessionStore, IOpportunityStoreSync
    {

        public SessionStoreSync(AppSettings appSettings) : base(appSettings)
        {

        }

        public void BookOrderItemsSync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.BookOrderItemsSync(ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }

        public void LeaseOrderItemsSync(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.LeaseOrderItemsSync(lease, ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }

        public void ProposeOrderItemsSync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.ProposeOrderItemsSync(ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }
    }

    class SessionStoreAsync : SessionStore, IOpportunityStoreAsync
    {

        public SessionStoreAsync(AppSettings appSettings) : base(appSettings)
        {

        }

        public async Task BookOrderItemsAsync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.BookOrderItemsSync(ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }

        public async Task LeaseOrderItemsAsync(Lease lease, List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.LeaseOrderItemsSync(lease, ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }

        public async Task ProposeOrderItemsAsync(List<IOrderItemContext> orderItemContexts, StoreBookingFlowContext flowContext, IStateContext stateContext, IDatabaseTransaction databaseTransactionContext)
        {
            base.ProposeOrderItemsSync(ConvertToSpecificComponents(orderItemContexts), flowContext, (OrderStateContext)stateContext, (OrderTransaction)databaseTransactionContext);
        }
    }
}
