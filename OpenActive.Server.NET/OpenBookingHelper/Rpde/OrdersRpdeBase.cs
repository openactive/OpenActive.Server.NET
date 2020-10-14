using System;
using System.Collections.Generic;
using OpenActive.NET.Rpde.Version1;

// TODO: Consolidate this logic with RpdeBase.cs to remove duplication (using generics?)
namespace OpenActive.Server.NET.OpenBookingHelper
{
    public abstract class OrdersRpdeFeedGenerator : OrdersModelSupport, IRpdeFeedGenerator
    {
        public int RpdePageSize { get; private set; }

        protected Uri FeedUrl { get; private set; }

        internal void SetConfiguration(int rpdePageSize, OrderIdTemplate orderIdTemplate, SingleIdTemplate<SellerIdComponents> sellerIdTemplate, Uri offersFeedUrl)
        {
            base.SetConfiguration(orderIdTemplate, sellerIdTemplate);

            RpdePageSize = rpdePageSize;

            // Allow these to be overridden by implementations if customisation is required
            FeedUrl = offersFeedUrl;
        }

        /// <summary>
        /// This class is not designed to be used outside of the library, one of its subclasses must be used instead
        /// </summary>
        internal OrdersRpdeFeedGenerator() { }
    }

    public abstract class OrdersRpdeFeedIncrementingUniqueChangeNumber : OrdersRpdeFeedGenerator, IRpdeOrdersFeedIncrementingUniqueChangeNumber
    {
        protected abstract List<RpdeItem> GetRpdeItems(string clientId, long? afterChangeNumber);

        public RpdePage GetOrdersRpdePage(string clientId, long? afterChangeNumber)
        {
            return new RpdePage(FeedUrl, afterChangeNumber, GetRpdeItems(clientId, afterChangeNumber));
        }
    }

    public abstract class OrdersRpdeFeedModifiedTimestampAndId : OrdersRpdeFeedGenerator, IRpdeOrdersFeedModifiedTimestampAndIdString
    {
        protected abstract List<RpdeItem> GetRpdeItems(string clientId, long? afterTimestamp, string afterId);

        public RpdePage GetOrdersRpdePage(string clientId, long? afterTimestamp, string afterId)
        {
            if (!afterTimestamp.HasValue && !string.IsNullOrWhiteSpace(afterId) || afterTimestamp.HasValue && string.IsNullOrWhiteSpace(afterId))
                throw new ArgumentNullException(nameof(afterTimestamp), "afterTimestamp and afterId must both be supplied, or neither supplied");

            return new RpdePage(FeedUrl, afterTimestamp, afterId, GetRpdeItems(clientId, afterTimestamp, afterId));
        }
    }
}
