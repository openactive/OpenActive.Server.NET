using OpenActive.Server.NET.OpenBookingHelper;
using System;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace BookingSystem
{
    public class AcmeIdempotencyStore : IdempotencyStore
    {
        private readonly ObjectCache _cache = MemoryCache.Default;

        protected override ValueTask<string> GetSuccessfulOrderCreationResponse(string idempotencyKey)
        {
            return new ValueTask<string>((string)_cache.Get(idempotencyKey));
        }

        protected override ValueTask SetSuccessfulOrderCreationResponse(string idempotencyKey, string responseJson)
        {
            var policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5);
            _cache.Set(idempotencyKey, responseJson, policy);
            return new ValueTask();
        }
    }
}