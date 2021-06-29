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
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var grants = await FakeBookingSystem.Database.GetAllGrants(subjectId);
            var persistedGrants = grants.Select(grant => new PersistedGrant
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            }).ToList();

            return persistedGrants;
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var grant = await FakeBookingSystem.Database.GetGrant(key);

            return grant != null ? new PersistedGrant()
            {
                Key = grant.Key,
                Type = grant.Type,
                SubjectId = grant.SubjectId,
                ClientId = grant.ClientId,
                CreationTime = grant.CreationTime,
                Expiration = grant.Expiration,
                Data = grant.Data
            } : null;
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            FakeBookingSystem.Database.RemoveGrant(subjectId, clientId);
            return Task.CompletedTask;
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            FakeBookingSystem.Database.RemoveGrant(subjectId, clientId, type);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            FakeBookingSystem.Database.RemoveGrant(key);
            return Task.CompletedTask;
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            FakeBookingSystem.Database.AddGrant(grant.Key, grant.Type, grant.SubjectId, grant.ClientId, grant.CreationTime, grant.Expiration, grant.Data);
            return Task.CompletedTask;
        }
    }
}
