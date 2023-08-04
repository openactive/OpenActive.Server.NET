using OpenActive.NET;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public abstract class IdempotencyStore
    {
        protected abstract ValueTask<string> GetSuccessfulOrderCreationResponse(string idempotencyKey);
        protected abstract ValueTask SetSuccessfulOrderCreationResponse(string idempotencyKey, string responseJson);

        internal ValueTask<string> GetSuccessfulOrderCreationResponse(OrderIdComponents orderId, string orderJson) {
            return GetSuccessfulOrderCreationResponse(CalculateIdempotencyKey(orderId, orderJson));
        }

        internal ValueTask SetSuccessfulOrderCreationResponse(OrderIdComponents orderId, string orderJson, string responseJson) {
            return SetSuccessfulOrderCreationResponse(CalculateIdempotencyKey(orderId, orderJson), responseJson);
        }

        protected string CalculateIdempotencyKey(OrderIdComponents orderId, string orderJson) {
            return $"{orderId.ClientId}|{orderId.uuid}|{orderId.OrderType.ToString()}|{ComputeSHA256Hash(orderJson)}";
        }
        
        protected static string ComputeSHA256Hash(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
            }                
        }
    }
}