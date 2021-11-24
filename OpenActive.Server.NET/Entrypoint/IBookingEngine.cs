using System;
using System.Threading.Tasks;
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
        Task<ResponseContent> RenderDatasetSite();
        Task<ResponseContent> GetOpenDataRPDEPageForFeed(string feedname, long? afterTimestamp, string afterId, long? afterChangeNumber);
        Task<ResponseContent> GetOpenDataRPDEPageForFeed(string feedname, string afterTimestamp, string afterId, string afterChangeNumber);

        // These endpoints are authenticated by seller credentials (OAuth Authorization Code Grant)
        Task<ResponseContent> ProcessCheckpoint1(string clientId, Uri sellerId, string uuid, string orderQuoteJson, Uri customerAccountId = null);
        Task<ResponseContent> ProcessCheckpoint2(string clientId, Uri sellerId, string uuid, string orderQuoteJson, Uri customerAccountId = null);
        Task<ResponseContent> ProcessOrderCreationB(string clientId, Uri sellerId, string uuid, string orderJson, Uri customerAccountId = null);
        Task<ResponseContent> ProcessOrderProposalCreationP(string clientId, Uri sellerId, string uuid, string orderJson, Uri customerAccountId = null);
        Task<ResponseContent> DeleteOrder(string clientId, Uri sellerId, string uuid, Uri customerAccountId = null);
        Task<ResponseContent> DeleteOrderQuote(string clientId, Uri sellerId, string uuid, Uri customerAccountId = null);
        Task<ResponseContent> ProcessOrderUpdate(string clientId, Uri sellerId, string uuid, string orderJson, Uri customerAccountId = null);
        Task<ResponseContent> ProcessOrderProposalUpdate(string clientId, Uri sellerId, string uuid, string orderJson, Uri customerAccountId = null);
        Task<ResponseContent> GetOrderStatus(string clientId, Uri sellerId, string uuid, Uri customerAccountId = null);

        // These endpoints are authenticated by client credentials (OAuth Client Credentials Grant)
        Task<ResponseContent> InsertTestOpportunity(string testDatasetIdentifier, string eventJson);
        Task<ResponseContent> DeleteTestDataset(string testDatasetIdentifier);
        Task<ResponseContent> TriggerTestAction(string actionJson);
        Task<ResponseContent> GetOrdersRPDEPageForFeed(string clientId, string afterTimestamp, string afterId, string afterChangeNumber);
        Task<ResponseContent> GetOrdersRPDEPageForFeed(string clientId, long? afterTimestamp, string afterId, long? afterChangeNumber);
        Task<ResponseContent> GetOrderProposalsRPDEPageForFeed(string clientId, string afterTimestamp, string afterId, string afterChangeNumber);
        Task<ResponseContent> GetOrderProposalsRPDEPageForFeed(string clientId, long? afterTimestamp, string afterId, long? afterChangeNumber);
    }
}