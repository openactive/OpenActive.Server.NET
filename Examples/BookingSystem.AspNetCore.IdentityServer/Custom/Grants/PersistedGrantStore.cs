using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class AcmePersistedGrantStore : IPersistedGrantStore
    {
        protected readonly ILogger _logger;
        private readonly FakeBookingSystem _fakeBookingSystem;

        public AcmePersistedGrantStore(ILogger<AcmePersistedGrantStore> logger, FakeBookingSystem fakeBookingSystem)
        {
            _logger = logger;
            _fakeBookingSystem = fakeBookingSystem;
        }

        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            var grants = await _fakeBookingSystem.database.GetAllGrants(filter.SubjectId, filter.SessionId, filter.ClientId, filter.Type);

            _logger.LogDebug("{persistedGrantCount} persisted grants found for {@filter}", grants.Count, filter);

            var persistedGrants = grants.Select(grant => new PersistedGrant
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                SessionId = grant.SessionId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                ConsumedTime = grant.ConsumedTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            }).ToList();

            return persistedGrants;
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var grant = await _fakeBookingSystem.database.GetGrant(key);

            _logger.LogDebug("{persistedGrantKey} found in database: {persistedGrantKeyFound}", key, grant != null);

            return grant != null ? new PersistedGrant
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                SessionId = grant.SessionId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                ConsumedTime = grant.ConsumedTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            } : null;
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            _logger.LogDebug("removing all persisted grants from database for {@filter}", filter);

            await _fakeBookingSystem.database.RemoveAllGrants(filter.SubjectId, filter.SessionId, filter.ClientId, filter.Type);
        }

        public async Task RemoveAsync(string key)
        {
            _logger.LogDebug("removing {persistedGrantKey} persisted grant from database", key);

            await _fakeBookingSystem.database.RemoveGrant(key);
        }

        public async Task StoreAsync(PersistedGrant grant)
        {
            if (await _fakeBookingSystem.database.AddGrant(grant.Key, grant.Type, grant.SubjectId, grant.SessionId, grant.ClientId, grant.CreationTime, grant.ConsumedTime, grant.Expiration, grant.Data))
            {
                _logger.LogDebug("{persistedGrantKey} not found in database, and so was inserted", grant.Key);
            }
            else
            {
                _logger.LogDebug("{persistedGrantKey} found in database, and updated", grant.Key);
            }
        }
    }
}
