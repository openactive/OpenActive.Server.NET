using System;
using System.Runtime.Serialization;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    // Note in future we may make these more flexible (and configurable), but for now they are set for the simple case

    public class SimpleIdComponents : IEquatable<SimpleIdComponents>
    {
        public long? IdLong { get; set; }
        public Guid? IdGuid { get; set; }
        public string IdString { get; set; }

        public bool Equals(SimpleIdComponents other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (other.IdLong != null && this.IdLong != null) return other.IdLong == this.IdLong;
            if (other.IdGuid != null && this.IdGuid != null) return other.IdGuid == this.IdGuid;
            if (other.IdString != null && this.IdString != null) return other.IdString == this.IdString;
            if (other.IdString == null && this.IdString == null && other.IdLong == null && this.IdLong == null) return true;
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
        public static bool operator ==(SimpleIdComponents left, SimpleIdComponents right) {
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
        public static bool operator !=(SimpleIdComponents left, SimpleIdComponents right) => !(left == right);

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => this.Equals(obj as SimpleIdComponents);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => Schema.NET.HashCode.Of(this.IdLong).And(this.IdString);
    }

    public class OrderIdComponents
    {
        public OrderType? OrderType { get; set; }
        public string ClientId { get; set; }
        public Guid uuid { get; set; }
        public long? OrderItemIdLong { get; set; }
        public string OrderItemIdString { get; set; }
        public Guid? OrderItemIdGuid { get; set; }
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
