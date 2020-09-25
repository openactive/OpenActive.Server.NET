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
    class FacilityStore : OpportunityStore<FacilityOpportunity, OrderTransaction, OrderStateContext>
    {

        protected override FacilityOpportunity CreateOpportunityWithinTestDataset(string testDatasetIdentifier, OpportunityType opportunityType, TestOpportunityCriteriaEnumeration criteria, SellerIdComponents seller)
        {
            switch (opportunityType)
            {
                case OpportunityType.FacilityUseSlot:
                    switch (criteria)
                    {
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableCancellable:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookablePaid:
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookable:
                            var (facilityId1, slotId1) = FakeBookingSystem.Database.AddFacility(testDatasetIdentifier, seller.SellerIdLong.Value, "[OPEN BOOKING API TEST INTERFACE] Bookable Paid Event", 14.99M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 10);
                            return new FacilityOpportunity
                            {
                                OpportunityType = opportunityType,
                                FacilityUseId = facilityId1,
                                SlotId = slotId1
                            };
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableFree:
                            var (facilityId2, slotId2) = FakeBookingSystem.Database.AddFacility(testDatasetIdentifier, seller.SellerIdLong.Value, "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event", 0M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 10);
                            return new FacilityOpportunity
                            {
                                OpportunityType = opportunityType,
                                FacilityUseId = facilityId2,
                                SlotId = slotId2
                            };
                        case TestOpportunityCriteriaEnumeration.TestOpportunityBookableNoSpaces:
                            var (facilityId3, slotId3) = FakeBookingSystem.Database.AddFacility(testDatasetIdentifier, seller.SellerIdLong.Value, "[OPEN BOOKING API TEST INTERFACE] Bookable Free Event", 14.99M, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(1).AddHours(1), 0);
                            return new FacilityOpportunity
                            {
                                OpportunityType = opportunityType,
                                FacilityUseId = facilityId3,
                                SlotId = slotId3
                            };
                        default:
                            throw new OpenBookingException(new OpenBookingError(), "testOpportunityCriteria value not supported");
                    }


                default:
                    throw new OpenBookingException(new OpenBookingError(), "Opportunity Type not supported");
            }
        }

        protected override void DeleteTestDataset(string testDatasetIdentifier)
        {
            FakeBookingSystem.Database.DeleteTestFacilitiesFromDataset(testDatasetIdentifier);
        }

        protected override void TriggerTestAction(OpenBookingSimulateAction simulateAction, FacilityOpportunity idComponents)
        {
            throw new NotImplementedException();
        }


        // Similar to the RPDE logic, this needs to render and return an new hypothetical OrderItem from the database based on the supplied opportunity IDs
        protected override void GetOrderItems(List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext)
        {

            // Note the implementation of this method must also check that this OrderItem is from the Seller specified by context.SellerIdComponents (this is not required if using a Single Seller)

            // Additionally this method must check that there are enough spaces in each entry

            // Response OrderItems must be updated into supplied orderItemContexts (including duplicates for multi-party booking)

            List<SlotTable> slotTable;
            List<FacilityUseTable> facilityTable;
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                slotTable = db.Select<SlotTable>();
                facilityTable = db.Select<FacilityUseTable>();
            }

            var query = (from orderItemContext in orderItemContexts
                         join slot in slotTable on orderItemContext.RequestBookableOpportunityOfferId.SlotId equals slot.Id
                         join facility in facilityTable on slot.FacilityUseId equals facility.Id
                         // and offers.id = opportunityOfferId.OfferId
                         select slot == null ? null : new {
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
                                            Price = facility.Price * (decimal?)0.2,
                                            PriceCurrency = "GBP",
                                            Rate = (decimal?)0.2
                                        }
                                     } : null,
                                 AcceptedOffer = new Offer
                                 {
                                     // Note this should always use RenderOfferId with the supplied SessioFacilityOpportunitynOpportunity, to take into account inheritance and OfferType
                                     Id = this.RenderOfferId(orderItemContext.RequestBookableOpportunityOfferId),
                                     Price = facility.Price,
                                     PriceCurrency = "GBP"
                                 },
                                 OrderedItem = new Slot
                                 {
                                     // Note this should always be driven from the database, with new FacilityOpportunity's instantiated
                                     Id = this.RenderOpportunityId(new FacilityOpportunity
                                     {
                                         OpportunityType = OpportunityType.FacilityUseSlot,
                                         FacilityUseId = slot.FacilityUseId,
                                         SlotId = slot.Id
                                     }),
                                     FacilityUse = new FacilityUse
                                     {
                                         Id = this.RenderOpportunityId(new FacilityOpportunity
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
                                     StartDate = (DateTimeOffset)slot.Start,
                                     EndDate = (DateTimeOffset)slot.End,
                                     MaximumUses = slot.MaximumUses,
                                     RemainingUses= slot.RemainingUses
                                 }
                             },
                             SellerId = new SellerIdComponents { SellerIdLong = facility.SellerId }
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

                    if (((Slot)item.OrderItem.OrderedItem).RemainingUses == 0)
                    {
                        ctx.AddError(new OpportunityIsFullError());
                    }
                }
                
            }

            // Add errors to the response according to the attendee details specified as required in the ResponseOrderItem,
            // and those provided in the requestOrderItem
            orderItemContexts.ForEach(ctx => ctx.ValidateAttendeeDetails());

            // Additional attendee detail validation logic goes here
            // ...

        }

        protected override void LeaseOrderItems(Lease lease, List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
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
                    bool result = databaseTransaction.Database.LeaseOrderItemsForFacilitySlot(flowContext.OrderId.ClientId, flowContext.SellerId.SellerIdLong ?? null /* Hack to allow this to work in Single Seller mode too */, flowContext.OrderId.uuid, ctxGroup.Key.SlotId.Value, ctxGroup.Count());

                    if (!result)
                    {
                        foreach (var ctx in ctxGroup)
                        {
                            ctx.AddError(new OpportunityIntractableError(), "OrderItem could not be leased for unexpected reasons.");
                        }
                    }
                }
            }
        }

        //TODO: This should reuse code of LeaseOrderItem
        protected override void BookOrderItems(List<OrderItemContext<FacilityOpportunity>> orderItemContexts, StoreBookingFlowContext flowContext, OrderStateContext stateContext, OrderTransaction databaseTransaction)
        {
            // Check that there are no conflicts between the supplied opportunities
            // Also take into account spaces requested across OrderItems against total spaces in each opportunity

            foreach (var ctxGroup in orderItemContexts.GroupBy(x => x.RequestBookableOpportunityOfferId))
            {
                // Check that the Opportunity ID and type are as expected for the store 
                if (ctxGroup.Key.OpportunityType != OpportunityType.FacilityUseSlot || !ctxGroup.Key.SlotId.HasValue)
                {
                    throw new OpenBookingException(new UnableToProcessOrderItemError());
                }

                // Attempt to book for those with the same IDs, which is atomic
                List<long> orderItemIds = databaseTransaction.Database.BookOrderItemsForFacilitySlot(flowContext.OrderId.ClientId, flowContext.SellerId.SellerIdLong ?? null  /* Hack to allow this to work in Single Seller mode too */, flowContext.OrderId.uuid, ctxGroup.Key.SlotId.Value, this.RenderOpportunityJsonLdType(ctxGroup.Key), this.RenderOpportunityId(ctxGroup.Key).ToString(), this.RenderOfferId(ctxGroup.Key).ToString(), ctxGroup.Count());

                if (orderItemIds != null)
                {
                    // Set OrderItemId for each orderItemContext
                    foreach (var (ctx, id) in ctxGroup.Zip(orderItemIds, (ctx, id) => (ctx, id)))
                    {
                        ctx.SetOrderItemId(flowContext, id);
                    }
                }
                else
                {
                    // Note: A real implementation would not through an error this vague
                    throw new OpenBookingException(new OrderCreationFailedError(), "Booking failed for an unexpected reason");
                }
            }
        }

    }

}
