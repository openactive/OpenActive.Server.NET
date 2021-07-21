using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using OpenActive.FakeDatabase.NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class AcmePersistedGrantStore : IPersistedGrantStore
    {
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            var grants = await FakeBookingSystem.FakeDatabase.GetAllGrants(filter.SubjectId, filter.SessionId, filter.ClientId, filter.Type);
            var persistedGrants = grants.Select(grant => new PersistedGrant
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                SessionId = grant.SessionId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            }).ToList();

            return persistedGrants;
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var grant = await FakeBookingSystem.FakeDatabase.GetGrant(key);

            return grant != null ? new PersistedGrant
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                SessionId = grant.SessionId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            } : null;
        }

        public async Task RemoveAllAsync(PersistedGrantFilter filter)
        {
            filter.Validate();

            await FakeBookingSystem.FakeDatabase.RemoveAllGrants(filter.SubjectId, filter.SessionId, filter.ClientId, filter.Type);
        }

        public async Task RemoveAsync(string key)
        {
            await FakeBookingSystem.FakeDatabase.RemoveGrant(key);
        }
        
        public async Task StoreAsync(PersistedGrant grant)
        {
            await FakeBookingSystem.FakeDatabase.AddGrant(grant.Key, grant.Type, grant.SubjectId, grant.SessionId, grant.ClientId, grant.CreationTime, grant.Expiration, grant.Data);
        }
    }
}
