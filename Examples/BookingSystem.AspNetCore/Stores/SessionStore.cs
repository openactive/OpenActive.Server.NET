using OpenActive.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenActive.DatasetSite.NET;
using OpenActive.Server.NET.StoreBooking;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.FakeDatabase.NET;
using ServiceStack.OrmLite;

namespace BookingSystem
{
    class SessionStore : OpportunityStore<SessionOpportunity, OrderTransaction, OrderStateContext>
    {
        protected override SessionOpportunity CreateOpportunityWithinTestDataset(
            string testDatasetIdentifier,
            OpportunityType opportunityType,
            TestOpportunityCriteriaEnumeration criteria,
            SellerIdComponents seller)
        {
            if (!seller.SellerIdLong.HasValue)
                throw new OpenBookingException(new OpenBookingError(), "Seller must have an ID");

            switch (opportunityType)
            {
                case OpportunityType.ScheduledSession:
                    switch (criteria)
                    {
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookablePaid:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookable:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event",
                                14.99M,
                                10);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableWithinValidFromBeforeStartDate:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event Within Window",
                                14.99M,
                                10,
                                validFromStartDate: true);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFree:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event",
                                0M,
                                10);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNoSpaces:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event No Spaces",
                                14.99M,
                                0);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFiveSpaces:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event Five Spaces",
                                14.99M,
                                5);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFlowRequirementOnlyApproval:
                        {
                            var (classId, occurrenceId) = FakeBookingSystem.Database.AddClass(
                                testDatasetIdentifier,
                                seller.SellerIdLong.Value,
                                "[OPEN BOOKING API TEST INTERFACE] Bookable Event With Approval",
                                14.99M,
                                10,
                                requiresApproval: true);
                            return new SessionOpportunity
                            {
                                OpportunityType = opportunityType,
                                SessionSeriesId = classId,
                                ScheduledSessionId = occurrenceId
                            };
                        }
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "testOpportunityCriteria value not supported");
                    }

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
            throw new NotImplementedException();
        }


        // Similar to the RPDE logic, this needs to render and return an new hypothetical OrderItem from the database based on the supplied opportunity IDs
        protected override void GetOrderItems(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
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
                             select occurrences == null ? null : new {
                                 OrderItem = new OrderItem
                                 {
                                     AllowCustomerCancellationFullRefund = true,
                                     // TODO: The static example below should come from the database (which doesn't currently support tax)
                                     UnitTaxSpecification = flowContext.TaxPayeeRelationship == TaxPayeeRelationship.BusinessToConsumer ?
                                         new List<TaxChargeSpecification>
                                         {
                                            new TaxChargeSpecification
                                            {
                                                Name = "VAT at 20%",
                                                Price = classes.Price * (decimal?)0.2,
                                                PriceCurrency = "GBP",
                                                Rate = (decimal?)0.2
                                            }
                                         } : null,
                                     AcceptedOffer = new Offer
                                     {
                                         // Note this should always use RenderOfferId with the supplied SessionOpportunity, to take into account inheritance and OfferType
                                         Id = RenderOfferId(orderItemContext.RequestBookableOpportunityOfferId),
                                         Price = classes.Price,
                                         PriceCurrency = "GBP",
                                         ValidFromBeforeStartDate = classes.ValidFromBeforeStartDate
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
                                     }
                                 },
                                 SellerId = new SellerIdComponents { SellerIdLong = classes.SellerId },
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

                        if (item.RequiresApproval) ctx.SetRequiresApproval();

                        if (item.OrderItem.OrderedItem.RemainingAttendeeCapacity == 0)
                        {
                            ctx.AddError(new OpportunityIsFullError());
                        }
                    }
                }
            }

            // Add errors to the response according to the attendee details specified as required in the ResponseOrderItem,
            // and those provided in the requestOrderItem
            orderItemContexts.ForEach(ctx => ctx.ValidateAttendeeDetails());

            // Additional attendee detail validation logic goes here
            // ...
        }

        protected override void LeaseOrderItems(Lease lease, List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
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
                        flowContext.SellerId.SellerIdLong /* Hack to allow this to work in Single Seller mode too */,
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
        protected override void BookOrderItems(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.ScheduledSession || !ctxGroup.Key.ScheduledSessionId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError());
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, orderItemIds) = FakeDatabase.BookOrderItemsForClassOccurrence(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.ScheduledSessionId.Value,
                    RenderOpportunityJsonLdType(ctxGroup.Key),
                    RenderOpportunityId(ctxGroup.Key).ToString(),
                    RenderOfferId(ctxGroup.Key).ToString(),
                    ctxGroup.Count(),
                    false);
                
                switch (result)
                {
                    case ReserveOrderItemsResult.Success:
                        // Set OrderItemId for each orderItemContext
                        foreach (var (ctx, id) in ctxGroup.Zip(orderItemIds, (ctx, id) => (ctx, id)))
                        {
                            ctx.SetOrderItemId(flowContext, id);
                        }
                        break;
                    case ReserveOrderItemsResult.SellerIdMismatch:
                        throw new OpenBookingException(new SellerMismatchError(), "An OrderItem SellerID did not match");
                    case ReserveOrderItemsResult.OpportunityNotFound:
                        throw new OpenBookingException(new UnableToProcessOrderItemError(), "Opportunity not found");
                    case ReserveOrderItemsResult.NotEnoughCapacity:
                        throw new OpenBookingException(new OpportunityHasInsufficientCapacityError());
                    case ReserveOrderItemsResult.OpportunityOfferPairNotBookable:
                        throw new OpenBookingException(new OpportunityOfferPairNotBookableError());
                    default:
                        throw new OpenBookingException(new OrderCreationFailedError(), "Booking failed for an unexpected reason");
                }
            }
        }

        // TODO check logic here, it's just been copied from BookOrderItems. Possibly could remove duplication here.
        protected override void ProposeOrderItems(List<OrderItemContext<SessionOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.ScheduledSession || !ctxGroup.Key.ScheduledSessionId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError());
                }

                // Attempt to book for those with the same IDs, which is atomic
                var (result, _) = FakeDatabase.BookOrderItemsForClassOccurrence(
                    databaseTransaction.FakeDatabaseTransaction,
                    flowContext.OrderId.ClientId,
                    flowContext.SellerId.SellerIdLong /* Hack to allow this to work in Single Seller mode too */,
                    flowContext.OrderId.uuid,
                    ctxGroup.Key.ScheduledSessionId.Value,
                    RenderOpportunityJsonLdType(ctxGroup.Key),
                    RenderOpportunityId(ctxGroup.Key).ToString(),
                    RenderOfferId(ctxGroup.Key).ToString(),
                    ctxGroup.Count(),
                    true);

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
                        throw new OpenBookingException(new OpportunityOfferPairNotBookableError());
                    default:
                        throw new OpenBookingException(new OrderCreationFailedError(), "Booking failed for an unexpected reason");
                }
            }
        }
    }

}
