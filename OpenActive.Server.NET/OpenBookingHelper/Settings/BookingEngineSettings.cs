using System;
using System.Collections.Generic;
using OpenActive.DatasetSite.NET;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    /// <summary>
    /// QUESTION: Should this be an interface? How do we use the settings pattern?
    /// </summary>
    public class BookingEngineSettings
    {
        /// <summary>
        /// This Dictionary maps pairs of JSON-LD IDs to strongly typed classes containing their components.
        /// It is used by the booking engine to validate and transform IDs provided by the Broker.
        /// 
        /// The classes are POCO simply implementing the IBookablePairIdTemplate interface.
        /// 
        /// The first ID is for the opportunity, the second ID is for the offer.
        /// </summary>
        public List<IBookablePairIdTemplate> IdConfiguration { get; set;  }
        public OrderIdTemplate OrderIdTemplate { get; set; }
        public SingleIdTemplate<SellerIdComponents> SellerIdTemplate { get; set; }
        public Dictionary<OpportunityType, IOpportunityDataRpdeFeedGenerator> OpenDataFeeds { get; set; }
        public int RPDEPageSize { get; set; } = 500;
        public Uri JsonLdIdBaseUrl { get; set; }
        public OrdersRPDEFeedGenerator OrdersFeedGenerator { get; set; }
        public OrdersRPDEFeedGenerator OrderProposalsFeedGenerator { get; set; }
        public SellerStore SellerStore { get; set; }
        public bool HasSingleSeller { get; set; }
    }
}
