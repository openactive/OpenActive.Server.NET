using System;
using ServiceStack.DataAnnotations;

namespace OpenActive.FakeDatabase.NET
{
    public class OccurrenceTable : Table
    {
        public string TestDatasetIdentifier { get; set; }
        [Reference]
        public ClassTable ClassTable { get; set; }
        [ForeignKey(typeof(ClassTable), OnDelete = "CASCADE")]
        public long ClassId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long TotalSpaces { get; set; }
        public long LeasedSpaces { get; set; }
        public long RemainingSpaces { get; set; }
    }
}