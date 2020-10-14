using System;
using System.Runtime.Serialization;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    // Note in future we may make these more flexible (and configurable), but for now they are set for the simple case

    public class SellerIdComponents : IEquatable<SellerIdComponents>
    {
        public long? SellerIdLong { get; set; }
        public string SellerIdString { get; set; }

        public bool Equals(SellerIdComponents other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (other.SellerIdLong != null && SellerIdLong != null) return other.SellerIdLong == SellerIdLong;
            if (other.SellerIdString != null && SellerIdString != null) return other.SellerIdString == SellerIdString;
            if (other.SellerIdString == null && SellerIdString == null && other.SellerIdLong == null && SellerIdLong == null) return true;
            return false;                
        }
        
        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(SellerIdComponents left, SellerIdComponents right) {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }
            if (ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(SellerIdComponents left, SellerIdComponents right) => !(left == right);

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => Equals(obj as SellerIdComponents);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => Schema.NET.HashCode.Of(SellerIdLong).And(SellerIdString);
    }

    public class OrderIdComponents
    {
        public OrderType? OrderType { get; set; }
        public string ClientId { get; set; }
        public string uuid { get; set; }
        public long? OrderItemIdLong { get; set; }
        public string OrderItemIdString { get; set; }
    }

    // TODO: Add resolve Order ID via enumeration, and add paths (e.g. 'order-quote-template') to the below
    public enum OrderType {
        [EnumMember(Value = "order-quotes")]
        OrderQuote,

        [EnumMember(Value = "order-proposals")]
        OrderProposal,

        [EnumMember(Value = "orders")]
        Order
    }

}
