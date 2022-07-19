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

        private readonly AppSettings _appSettings;

        public AcmeOrdersFeedRpdeGenerator(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        protected override async Task<List<RpdeItem>> GetRPDEItems(string clientId, long? afterTimestamp, string afterId)
        {
            // Note if using SQL Server it is best to use rowversion as the modified value for the Orders table,
            // and update this class to inherit from OrdersRPDEFeedIncrementingUniqueChangeNumber
            // (to use afterChangeNumber, instead of afterTimestamp and afterId)

            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                long afterTimestampLong = afterTimestamp ?? 0;
                var q = db.From<OrderTable>()
                .LeftJoin<OrderTable, OrderItemsTable>((orders, items) => orders.OrderId == items.OrderId)
                .OrderBy(x => x.OrderModified)
                .ThenBy(x => x.OrderId)
                .Where(x =>
                    x.VisibleInOrdersFeed != FeedVisibility.None && x.ClientId == clientId && (
                        x.OrderModified > afterTimestampLong ||
                        (x.OrderModified == afterTimestamp &&
                        // Note this comparison will fail OpenActive validation if using SQL Server GUIDs due to the SQL Server GUID ordering
                        // Use SQL Server rowversion and OrdersRPDEFeedIncrementingUniqueChangeNumber instead
                        string.Compare(afterId, x.OrderId, StringComparison.InvariantCulture) > 0)) &&
                    x.OrderModified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
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
                        Modified = result.OrderTable.OrderModified,
                        State = result.OrderTable.Deleted || result.OrderTable.VisibleInOrdersFeed == FeedVisibility.Archived ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.OrderTable.Deleted || result.OrderTable.VisibleInOrdersFeed == FeedVisibility.Archived ? null :
                            AcmeOrderStore.RenderOrderFromDatabaseResult(RenderOrderId(result.OrderTable.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : OrderType.Order, new Guid(result.OrderTable.OrderId)), result.OrderTable, _appSettings.FeatureFlags.PrepaymentAlwaysRequired,
                                result.OrderItemsTable.Select(orderItem => new OrderItem
                                {
                                    Id = RenderOrderItemId(OrderType.Order, new Guid(result.OrderTable.OrderId), orderItem.Id),
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
                                        orderItem.Status == BookingStatus.Attended ? OrderItemStatus.AttendeeAttended :
                                        orderItem.Status == BookingStatus.Absent ? OrderItemStatus.AttendeeAbsent : (OrderItemStatus?)null,
                                    CancellationMessage = orderItem.CancellationMessage,
                                    CustomerNotice = orderItem.CustomerNotice,
                                }).ToList()
                            )
                    });

                return query.ToList();
            }
        }
    }

    public class AcmeOrderProposalsFeedRpdeGenerator : OrdersRPDEFeedModifiedTimestampAndID
    {
        //public override string FeedPath { get; protected set; } = "example path override";

        private readonly AppSettings _appSettings;

        public AcmeOrderProposalsFeedRpdeGenerator(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        protected override async Task<List<RpdeItem>> GetRPDEItems(string clientId, long? afterTimestamp, string afterId)
        {
            using (var db = FakeBookingSystem.Database.Mem.Database.Open())
            {
                long afterTimestampLong = afterTimestamp ?? 0;
                var q = db.From<OrderTable>()
                .LeftJoin<OrderTable, OrderItemsTable>((orders, items) => orders.OrderId == items.OrderId)
                .OrderBy(x => x.OrderProposalModified)
                .ThenBy(x => x.OrderId)
                .Where(x =>
                    x.VisibleInOrderProposalsFeed != FeedVisibility.None && x.ClientId == clientId && (
                        x.OrderProposalModified > afterTimestampLong ||
                        (x.OrderProposalModified == afterTimestamp &&
                        // Note this comparison will fail OpenActive validation if using SQL Server GUIDs due to the SQL Server GUID ordering
                        // Use SQL Server rowversion and OrdersRPDEFeedIncrementingUniqueChangeNumber instead
                        string.Compare(afterId, x.OrderId, StringComparison.InvariantCulture) > 0)) &&
                    x.OrderProposalModified < (DateTimeOffset.UtcNow - new TimeSpan(0, 0, 2)).UtcTicks)
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
                        Modified = result.OrderTable.OrderProposalModified,
                        State = result.OrderTable.Deleted || result.OrderTable.VisibleInOrderProposalsFeed == FeedVisibility.Archived ? RpdeState.Deleted : RpdeState.Updated,
                        Data = result.OrderTable.Deleted || result.OrderTable.VisibleInOrderProposalsFeed == FeedVisibility.Archived ? null :
                            AcmeOrderStore.RenderOrderFromDatabaseResult(RenderOrderId(result.OrderTable.OrderMode == OrderMode.Proposal ? OrderType.OrderProposal : OrderType.Order, new Guid(result.OrderTable.OrderId)), result.OrderTable, _appSettings.FeatureFlags.PrepaymentAlwaysRequired,
                                result.OrderItemsTable.Select(orderItem => new OrderItem
                                {
                                    Id = RenderOrderItemId(OrderType.Order, new Guid(result.OrderTable.OrderId), orderItem.Id),
                                    AcceptedOffer = new Offer
                                    {
                                        Id = orderItem.OfferJsonLdId,
                                        Price = orderItem.Price,
                                        PriceCurrency = "GBP"
                                    },
                                    OrderedItem = orderItem.OpportunityJsonLdId
                                }).ToList()
                            )
                    });

                return query.ToList();
            }
        }
    }
}
