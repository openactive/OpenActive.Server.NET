using Xunit;
using OpenActive.Server.NET.OpenBookingHelper;

namespace OpenActive.Server.NET.Tests
{
    public class SimpleIdComponentsTest
    {

        [Fact]
        public void SimpleIdComponents_Long_Equality()
        {
            var x = new SimpleIdComponents { IdLong = 0 };
            var y = new SimpleIdComponents { IdLong = 0 };

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SimpleIdComponents_String_Equality()
        {
            var x = new SimpleIdComponents { IdString = "abc" };
            var y = new SimpleIdComponents { IdString = "abc" };

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SimpleIdComponents_Long_Inequality()
        {
            var x = new SimpleIdComponents { IdLong = 0 };
            var y = new SimpleIdComponents { IdLong = 1 };

            Assert.False(x == y);
            Assert.True(x != y);
        }

        [Fact]
        public void SimpleIdComponents_String_Inequality()
        {
            var x = new SimpleIdComponents { IdString = "abc" };
            var y = new SimpleIdComponents { IdString = "def" };

            Assert.False(x == y);
            Assert.True(x != y);
        }

        [Fact]
        public void SimpleIdComponents_Null_Equality()
        {
            SimpleIdComponents x = null;
            SimpleIdComponents y = null;

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SimpleIdComponents_Null_Inequality()
        {
            SimpleIdComponents x = new SimpleIdComponents { IdString = "abc" };
            SimpleIdComponents y = null;

            Assert.False(x == y);
            Assert.True(x != y);
        }
    }
}