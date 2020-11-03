using System;
using System.Collections.Generic;
using System.Linq;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.NET.Rpde.Version1;
using OpenActive.NET;
using OpenActive.FakeDatabase.NET;
using ServiceStack.OrmLite;

namespace BookingSystem
{
    public class AcmeOrdersFeedRpdeGenerator : OrdersRPDEFeedModifiedTimestampAndID
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        protected override List<RpdeItem> GetRPDEItems(string clientId, long? afterTimestamp, string afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<OrderTable>()
                .Join<SellerTable>()
                .Join<OrderTable, OrderItemsTable>((orders, items) => orders.OrderId == items.OrderId)
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.OrderId)
                .Where(x =>
                    x.VisibleInFeed && x.ClientId == clientId && (
                        !afterTimestamp.HasValue ||
                        x.Modified > afterTimestamp ||
                        x.Modified == afterTimestamp &&
                        string.Compare(afterId, x.OrderId, StringComparison.InvariantCulture) > 0) &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RPDEPageSize);

                var query = db
                    .SelectMulti<OrderTable, SellerTable, OrderItemsTable>(q)
                    .GroupBy(x => new { x.Item1.OrderId })
                    .Select(result => new
                    {
                        OrderTable = result.Select(item => new { item.Item1 }).FirstOrDefault()?.Item1,
                        Seller = result.Select(item => new { item.Item2 }).FirstOrDefault()?.Item2,
                        OrderItemsTable = result.Select(item => new { item.Item3 }).ToList().Select(orderItem => orderItem.Item3).ToList()
                    })
                    .Select(result => new RpdeItem
                    {
                        Kind = RpdeKind.Order,
                        Id = result.OrderTable.OrderId,
                        Modified = result.OrderTable.Modified,
                        State = result.OrderTable.Deleted ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.OrderTable.Deleted ? null :
                            AcmeOrderStore.RenderOrderFromDatabaseResult(RenderOrderId(result.OrderTable.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : OrderType.Order, result.OrderTable.OrderId), result.OrderTable,
                                result.OrderItemsTable.Select(orderItem => new OrderItem
                                {
                                    Id = result.OrderTable.OrderMode == OrderMode.Booking ? RenderOrderItemId(OrderType.Order, result.OrderTable.OrderId, orderItem.Id) : null,
                                    AcceptedOffer = new Offer
                                    {
                                        Id = new Uri(orderItem.OfferJsonLdId),
                                        Price = orderItem.Price,
                                        PriceCurrency = "GBP"
                                    },
                                    OrderedItem = RenderOpportunityWithOnlyId(orderItem.OpportunityJsonLdType, new Uri(orderItem.OpportunityJsonLdId)),
                                    AccessCode = new List<PropertyValue>
                                    {
                                        new PropertyValue()
                                        {
                                            Name = "Pin Code",
                                            Description = orderItem.PinCode,
                                            Value = "defaultValue"
                                        }
                                    },
                                    OrderItemStatus =
                                        orderItem.Status == BookingStatus.Confirmed ? OrderItemStatus.OrderItemConfirmed :
                                        orderItem.Status == BookingStatus.CustomerCancelled ? OrderItemStatus.CustomerCancelled :
                                        orderItem.Status == BookingStatus.SellerCancelled ? OrderItemStatus.SellerCancelled :
                                        orderItem.Status == BookingStatus.Attended ? OrderItemStatus.CustomerAttended :
                                        orderItem.Status == BookingStatus.Proposed ? OrderItemStatus.OrderItemProposed : (OrderItemStatus?)null,
                                    CancellationMessage = orderItem.CancellationMessage
                                }).ToList()
                            )
                    });

                return query.ToList();
            }  
        }
    }
}
