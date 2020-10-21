using System;
using System.Collections.Generic;
using Bogus;

namespace OpenActive.FakeDatabase.NET.Helpers
{
    public static class FakerExtensions
    {
        public static IReadOnlyList<O> GenerateIntegerDistribution<I,O>(
            this Faker faker, int size, IReadOnlyList<I> bucketDefinitions, Func<Faker, int, I,O> transform)
        {
            var index = 0;
            var itemsPerBucket = size / bucketDefinitions.Count;
            var remainder = size % bucketDefinitions.Count;

            var entries = new List<O> ();
            for (var i = 0; i < bucketDefinitions.Count; i++)
            {
                var input = bucketDefinitions[i];

                if (i == bucketDefinitions.Count - 1) // last bucket
                    itemsPerBucket += remainder;

                for (var _ = 0; _ < itemsPerBucket; _++)
                {
                    var entry = transform(faker, index++, input);
                    entries.Add(entry);
                }
            }

            return entries.ToArray();
        }

        public static int Int(this Randomizer random, Bounds bounds)
        {
            return random.Int(bounds.Lower, bounds.Upper);
        }
    }
}