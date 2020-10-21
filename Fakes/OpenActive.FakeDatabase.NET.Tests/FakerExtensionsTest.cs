using System.Linq;
using Bogus;
using OpenActive.FakeDatabase.NET.Helpers;
using Xunit;

namespace OpenActive.FakeDatabase.NET.Test
{
    public class FakerExtensionsTest
    {
        [Fact]
        public void GenerateIntegerDistribution()
        {
            var bucketDefinitions = new[]
            {
                new Bounds(1, 5),
                new Bounds(6, 10),
                new Bounds(11, 15)
            };

            var distribution = new Faker().GenerateIntegerDistribution(11, bucketDefinitions, (Faker faker, int index, Bounds bounds) => faker.Random.Int(bounds));

            Assert.Equal(11, distribution.Count);
            Assert.True(distribution.Take(3).All(x => x >= 1 && x <= 5));
            Assert.True(distribution.Skip(3).Take(3).All(x => x >= 6 && x <= 10));
            Assert.True(distribution.Skip(6).Take(5).All(x => x >= 11 && x <= 15));
        }
    }
}