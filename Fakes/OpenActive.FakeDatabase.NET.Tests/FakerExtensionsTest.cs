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

            var distribution = new Faker().GenerateIntegerDistribution(11, bucketDefinitions);

            Assert.Equal(3, distribution[0].Count);
            Assert.True(distribution[0].All(x => x >= 1 && x <= 5));

            Assert.Equal(3, distribution[1].Count);
            Assert.True(distribution[1].All(x => x >= 6 && x <= 10));

            Assert.Equal(5, distribution[2].Count);
            Assert.True(distribution[2].All(x => x >= 11 && x <= 15));
        }
    }
}