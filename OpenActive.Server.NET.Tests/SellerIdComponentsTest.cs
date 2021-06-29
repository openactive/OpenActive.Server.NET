using Xunit;
using OpenActive.Server.NET.OpenBookingHelper;

namespace OpenActive.Server.NET.Tests
{
    public class SellerIdComponentsTest
    {

        [Fact]
        public void SellerIdComponents_Long_Equality()
        {
            var x = new SellerIdComponents { SellerIdLong = 0 };
            var y = new SellerIdComponents { SellerIdLong = 0 };

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SellerIdComponents_String_Equality()
        {
            var x = new SellerIdComponents { SellerIdString = "abc" };
            var y = new SellerIdComponents { SellerIdString = "abc" };

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SellerIdComponents_Long_Inequality()
        {
            var x = new SellerIdComponents { SellerIdLong = 0 };
            var y = new SellerIdComponents { SellerIdLong = 1 };

            Assert.False(x == y);
            Assert.True(x != y);
        }

        [Fact]
        public void SellerIdComponents_String_Inequality()
        {
            var x = new SellerIdComponents { SellerIdString = "abc" };
            var y = new SellerIdComponents { SellerIdString = "def" };

            Assert.False(x == y);
            Assert.True(x != y);
        }

        [Fact]
        public void SellerIdComponents_Null_Equality()
        {
            SellerIdComponents x = null;
            SellerIdComponents y = null;

            Assert.True(x == y);
            Assert.False(x != y);
        }

        [Fact]
        public void SellerIdComponents_Null_Inequality()
        {
            SellerIdComponents x = new SellerIdComponents { SellerIdString = "abc" };
            SellerIdComponents y = null;

            Assert.False(x == y);
            Assert.True(x != y);
        }
    }
}