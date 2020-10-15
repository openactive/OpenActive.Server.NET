using System;
using System.Collections.Generic;
using OpenActive.DatasetSite.NET;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;

// TODO: Consolidate this logic with RpdeBase.cs to remove duplication (using generics?)
namespace OpenActive.Server.NET.OpenBookingHelper
{
    public abstract class OrdersRPDEFeedGenerator : OrdersModelSupport, IRpdeFeedGenerator
    {
        public int RPDEPageSize { get; private set; }

        protected Uri FeedUrl { get; private set; }

        internal void SetConfiguration(int rpdePageSize, OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate, Uri offersFeedUrl)
        {
            base.SetConfiguration(orderIdTemplate, sellerIdTemplate);

            this.RPDEPageSize = rpdePageSize;

            // Allow these to be overridden by implementations if customisation is required
            this.FeedUrl = offersFeedUrl;
        }

        /// <summary>
        /// This class is not designed to be used outside of the library, one of its subclasses must be used instead
        /// </summary>
        internal OrdersRPDEFeedGenerator() { }
    }

    public abstract class OrdersRPDEFeedIncrementingUniqueChangeNumber : OrdersRPDEFeedGenerator, IRpdeOrdersFeedIncrementingUniqueChangeNumber
    {
        protected abstract List<RpdeItem> GetRPDEItems(string clientId, long? afterChangeNumber);

        public RpdePage GetOrdersRpdePage(string clientId, long? afterChangeNumber)
        {
            return new RpdePage(this.FeedUrl, afterChangeNumber, GetRPDEItems(clientId, afterChangeNumber));
        }
    }

    public abstract class OrdersRPDEFeedModifiedTimestampAndID : OrdersRPDEFeedGenerator, IRpdeOrdersFeedModifiedTimestampAndIdString
    {
        protected abstract List<RpdeItem> GetRPDEItems(string clientId, long? afterTimestamp, string afterId);

        public RpdePage GetOrdersRpdePage(string clientId, long? afterTimestamp, string afterId)
        {
            if ((!afterTimestamp.HasValue && !string.IsNullOrWhiteSpace(afterId)) ||
                (afterTimestamp.HasValue && string.IsNullOrWhiteSpace(afterId)))
            {
                throw new ArgumentNullException("afterTimestamp and afterId must both be supplied, or neither supplied");
            }
            else
            {
                return new RpdePage(this.FeedUrl, afterTimestamp, afterId, GetRPDEItems(clientId, afterTimestamp, afterId));
            }
        }
    }

}
