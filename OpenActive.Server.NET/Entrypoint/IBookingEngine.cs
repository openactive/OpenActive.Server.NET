using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenActive.NET;
using OpenActive.NET.Rpde.Version1;
using OpenActive.Server.NET.OpenBookingHelper;

namespace OpenActive.Server.NET
{
    /// <summary>
    /// This is the interface between the BookingEngine and the Web Framework (e.g. ASP.NET Core).
    /// 
    /// Note that this interface expects JSON requests to be supplied as strings, and provides JSON responses as strings.
    /// This ensures that deserialisation is always correct, regardless of the configuration of the web framework.
    /// It also removes the need to expose OpenActive (de)serialisation settings and parsers to the implementer, and makes
    /// this interface more maintainble as OpenActive.NET will likely upgrade to use the new System.Text.Json in time.
    /// </summary>
    public interface IBookingEngine
    {
        // These endpoints are fully open
        Task<ResponseContent> RenderDatasetSiteAsync();
        Task<ResponseContent> GetOpenDataRPDEPageForFeedAsync(string feedname, long? afterTimestamp, string afterId, long? afterChangeNumber);
        Task<ResponseContent> GetOpenDataRPDEPageForFeedAsync(string feedname, string afterTimestamp, string afterId, string afterChangeNumber);

        // These endpoints are authenticated by seller credentials (OAuth Authorization Code Grant)
        Task<ResponseContent> ProcessCheckpoint1Async(string clientId, Uri sellerId, string uuid, string orderQuoteJson);
        Task<ResponseContent> ProcessCheckpoint2Async(string clientId, Uri sellerId, string uuid, string orderQuoteJson);
        Task<ResponseContent> ProcessOrderCreationBAsync(string clientId, Uri sellerId, string uuid, string orderJson);
        Task<ResponseContent> ProcessOrderProposalCreationPAsync(string clientId, Uri sellerId, string uuid, string orderJson);
        ResponseContent DeleteOrder(string clientId, Uri sellerId, string uuid);
        ResponseContent DeleteOrderQuote(string clientId, Uri sellerId, string uuid);
        ResponseContent ProcessOrderUpdate(string clientId, Uri sellerId, string uuid, string orderJson);
        ResponseContent ProcessOrderProposalUpdate(string clientId, Uri sellerId, string uuid, string orderJson);

        // These endpoints are authenticated by client credentials (OAuth Client Credentials Grant)
        Task<ResponseContent> InsertTestOpportunityAsync(string testDatasetIdentifier, string eventJson);
        ResponseContent DeleteTestDataset(string testDatasetIdentifier);
        ResponseContent TriggerTestAction(string actionJson);
        Task<ResponseContent> GetOrdersRPDEPageForFeedAsync(string clientId, string afterTimestamp, string afterId, string afterChangeNumber);
        Task<ResponseContent> GetOrdersRPDEPageForFeedAsync(string clientId, long? afterTimestamp, string afterId, long? afterChangeNumber);
        Task<ResponseContent> GetOrderStatusAsync(string clientId, Uri sellerId, string uuid);
    }
}