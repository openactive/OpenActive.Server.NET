using System.Collections.Generic;
using Bogus;

namespace OpenActive.FakeDatabase.NET.Helpers
{
    public static class FakerExtensions
    {
        public static IReadOnlyList<IReadOnlyList<int>> GenerateIntegerDistribution(
            this Faker faker, int size, IReadOnlyList<Bounds> bucketDefinitions)
        {
            var itemsPerBucket = size / bucketDefinitions.Count;
            var remainder = size - itemsPerBucket * bucketDefinitions.Count;

            var buckets = new List<List<int>>();
            for (var i = 0; i < bucketDefinitions.Count; i++)
            {
                var range = bucketDefinitions[i];

                if (i == bucketDefinitions.Count - 1) // last bucket
                    itemsPerBucket += remainder;

                var entries = new List<int>();
                for (var _ = 0; _ < itemsPerBucket; _++)
                {
                    var entry = faker.Random.Int(range.Lower, range.Upper);
                    entries.Add(entry);
                }

                buckets.Add(entries);
            }

            return buckets.ToArray();
        }
    }
}
