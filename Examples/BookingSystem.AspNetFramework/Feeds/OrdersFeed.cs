using System;
using System.Collections.Generic;
using System.Linq;
using OpenActive.Server.NET.OpenBookingHelper;
using OpenActive.NET.Rpde.Version1;
using OpenActive.NET;
using OpenActive.FakeDatabase.NET;
using ServiceStack.OrmLite;
using System.Threading.Tasks;

namespace BookingSystem
{
    public class AcmeOrdersFeedRpdeGenerator : OrdersRPDEFeedModifiedTimestampAndID
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        protected async override Task<List<RpdeItem>> GetRPDEItems(string clientId, long? afterTimestamp, string afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                var q = db.From<OrderTable>()
                .LeftJoin<OrderTable, OrderItemsTable>((orders, items) => orders.OrderId == items.OrderId)
                .OrderBy(x => x.Modified)
                .ThenBy(x => x.OrderId)
                .Where(x =>
                    x.VisibleInFeed != FeedVisibility.None && x.ClientId == clientId && (
                        !afterTimestamp.HasValue ||
                        x.Modified > afterTimestamp ||
                        x.Modified == afterTimestamp &&
                        string.Compare(afterId, x.OrderId, StringComparison.InvariantCulture) > 0) &&
                    x.Modified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
                .Take(RPDEPageSize);

                var query = db
                    .SelectMulti<OrderTable, OrderItemsTable>(q)
                    .GroupBy(x => new { x.Item1.OrderId })
                    .Select(result => new
                    {
                        OrderTable = result.Select(item => new { item.Item1 }).FirstOrDefault()?.Item1,
                        OrderItemsTable = result.Select(item => new { item.Item2 }).ToList().Select(orderItem => orderItem.Item2).ToList()
                    })
                    .Select(result => new RpdeItem
                    {
                        Kind = RpdeKind.Order,
                        Id = result.OrderTable.OrderId,
                        Modified = result.OrderTable.Modified,
                        State = result.OrderTable.Deleted || result.OrderTable.VisibleInFeed == FeedVisibility.Archived ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.OrderTable.Deleted || result.OrderTable.VisibleInFeed == FeedVisibility.Archived ? null :
                            AcmeOrderStore.RenderOrderFromDatabaseResult(RenderOrderId(result.OrderTable.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : OrderType.Order, result.OrderTable.OrderId), result.OrderTable,
                                result.OrderItemsTable.Select(orderItem => new OrderItem
                                {
                                    Id = RenderOrderItemId(OrderType.Order, result.OrderTable.OrderId, orderItem.Id),
                                    AcceptedOffer = new Offer
                                    {
                                        Id = orderItem.OfferJsonLdId,
                                        Price = orderItem.Price,
                                        PriceCurrency = "GBP"
                                    },
                                    OrderedItem = orderItem.OpportunityJsonLdId,
                                    AccessChannel = orderItem.MeetingUrl != null ? new VirtualLocation()
                                    {
                                        Name = "Zoom Video Chat",
                                        Url = orderItem.MeetingUrl,
                                        AccessId = orderItem.MeetingId,
                                        AccessCode = orderItem.MeetingPassword,
                                        Description = "Please log into Zoom a few minutes before the event"
                                    } : null,
                                    AccessCode = orderItem.PinCode != null ? new List<PropertyValue>
                                    {
                                        new PropertyValue()
                                        {
                                            Name = "Pin Code",
                                            Description = orderItem.PinCode,
                                            Value = "defaultValue"
                                        }
                                    } : null,
                                    AccessPass = orderItem.BarCodeText != null ? new List<ImageObject>
                                    {
                                        new Barcode()
                                        {
                                            Url = new Uri(orderItem.ImageUrl),
                                            Text = orderItem.BarCodeText,
                                            CodeType = "code128"
                                        }
                                    } : null,
                                    OrderItemStatus =
                                        orderItem.Status == BookingStatus.Confirmed ? OrderItemStatus.OrderItemConfirmed :
                                        orderItem.Status == BookingStatus.CustomerCancelled ? OrderItemStatus.CustomerCancelled :
                                        orderItem.Status == BookingStatus.SellerCancelled ? OrderItemStatus.SellerCancelled :
                                        orderItem.Status == BookingStatus.Attended ? OrderItemStatus.CustomerAttended :
                                        orderItem.Status == BookingStatus.Proposed ? OrderItemStatus.OrderItemProposed : (OrderItemStatus?)null,
                                    CancellationMessage = orderItem.CancellationMessage,
                                    CustomerNotice = orderItem.CustomerNotice,
                                }).ToList()
                            )
                    });

                return query.ToList();
            }
        }
    }
}
