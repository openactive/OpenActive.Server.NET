using ServiceStack.DataAnnotations;
using System;

namespace OpenActive.FakeDatabase.NET
{
    public class GrantTable
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Type { get; set; }
        public string SubjectId { get; set; }
        public string SessionId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ConsumedTime { get; set; }
        public DateTime? Expiration { get; set; }
        public string Data { get; set; }
    }
}