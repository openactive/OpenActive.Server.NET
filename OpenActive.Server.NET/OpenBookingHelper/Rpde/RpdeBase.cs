using System;
using System.Collections.Generic;
using OpenActive.DatasetSite.NET;
using OpenActive.NET.Rpde.Version1;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public interface IOpportunityDataRpdeFeedGenerator : IRpdeFeedGenerator
    {
        string FeedPath { get; }
        void SetConfiguration(OpportunityTypeConfiguration opportunityTypeConfiguration, Uri jsonLdIdBaseUrl, int rpdePageSize, IBookablePairIdTemplate bookablePairIdTemplate, SingleIdTemplate<SellerIdComponents> sellerTemplate, Uri openDataFeedBaseUrl);
    }

    public abstract class OpportunityDataRpdeFeedGenerator<TComponents, TClass> : ModelSupport<TComponents>, IOpportunityDataRpdeFeedGenerator where TComponents : class, IBookableIdComponents, new() where TClass : Schema.NET.Thing
    {
        public int RpdePageSize { get; private set; }
        public virtual Uri FeedUrl { get; protected set; }
        public virtual string FeedPath { get; protected set; }

        public void SetConfiguration(OpportunityTypeConfiguration opportunityTypeConfiguration, Uri jsonLdIdBaseUrl, int rpdePageSize, IBookablePairIdTemplate bookablePairIdTemplate, SingleIdTemplate<SellerIdComponents> sellerTemplate, Uri openDataFeedBaseUrl)
        {
            if (!(bookablePairIdTemplate is BookablePairIdTemplate<TComponents>))
                throw new EngineConfigurationException($"{bookablePairIdTemplate?.GetType()} does not match {typeof(BookablePairIdTemplate<TComponents>)}. All types of IBookableIdComponents (T) used for BookablePairIdTemplate<T> assigned to feeds via settings.IdConfiguration must match those used for RPDEFeedGenerator<T> in settings.OpenDataFeeds.");

            if (opportunityTypeConfiguration?.SameAs.AbsolutePath.Trim('/') != typeof(TClass).Name)
                throw new EngineConfigurationException($"'{GetType()}' does not have this expected OpenActive model type as generic parameter: '{opportunityTypeConfiguration?.SameAs.AbsolutePath.Trim('/')}'");

            base.SetConfiguration((BookablePairIdTemplate<TComponents>)bookablePairIdTemplate, sellerTemplate);

            RpdePageSize = rpdePageSize;

            // Allow these to be overridden by implementations if customisation is required
            FeedUrl = FeedUrl ?? new Uri(openDataFeedBaseUrl + opportunityTypeConfiguration.DefaultFeedPath);
            FeedPath = FeedPath ?? opportunityTypeConfiguration.DefaultFeedPath;
        }

        /// <summary>
        /// This class is not designed to be used outside of the library, one of its subclasses must be used instead
        /// </summary>
        internal OpportunityDataRpdeFeedGenerator() { }
    }

    public abstract class RpdeFeedIncrementingUniqueChangeNumber<TComponents, TClass> : OpportunityDataRpdeFeedGenerator<TComponents, TClass>, IRpdeFeedIncrementingUniqueChangeNumber where TComponents : class, IBookableIdComponents, new() where TClass : Schema.NET.Thing
    {
        protected abstract List<RpdeItem<TClass>> GetRpdeItems(long? afterChangeNumber);

        public RpdePage GetRpdePage(long? afterChangeNumber)
        {
            return new RpdePage(FeedUrl, afterChangeNumber, GetRpdeItems(afterChangeNumber).ConvertAll(x => (RpdeItem)x));
        }
    }

    public abstract class RpdeFeedModifiedTimestampAndIdLong<TComponents, TClass> : OpportunityDataRpdeFeedGenerator<TComponents, TClass>, IRpdeFeedModifiedTimestampAndIdLong where TComponents : class, IBookableIdComponents, new() where TClass : Schema.NET.Thing
    {
        protected abstract List<RpdeItem<TClass>> GetRpdeItems(long? afterTimestamp, long? afterId);

        public RpdePage GetRpdePage(long? afterTimestamp, long? afterId)
        {
            if (!afterTimestamp.HasValue && afterId.HasValue || afterTimestamp.HasValue && !afterId.HasValue)
                throw new ArgumentException("afterTimestamp and afterId must both be supplied, or neither supplied");

            return new RpdePage(FeedUrl, afterTimestamp, afterId, GetRpdeItems(afterTimestamp, afterId).ConvertAll(x => (RpdeItem)x));
        }
    }

    public abstract class RpdeFeedModifiedTimestampAndIdString<TComponents, TClass> : OpportunityDataRpdeFeedGenerator<TComponents, TClass>, IRpdeFeedModifiedTimestampAndIdString where TComponents : class, IBookableIdComponents, new() where TClass : Schema.NET.Thing
    {
        protected abstract List<RpdeItem<TClass>> GetRpdeItems(long? afterTimestamp, string afterId);

        public RpdePage GetRpdePage(long? afterTimestamp, string afterId)
        {
            if (!afterTimestamp.HasValue && !string.IsNullOrWhiteSpace(afterId) || afterTimestamp.HasValue && string.IsNullOrWhiteSpace(afterId))
                throw new ArgumentException("afterTimestamp and afterId must both be supplied, or neither supplied");

            return new RpdePage(FeedUrl, afterTimestamp, afterId, GetRpdeItems(afterTimestamp, afterId).ConvertAll(x => (RpdeItem)x));
        }
    }
}
