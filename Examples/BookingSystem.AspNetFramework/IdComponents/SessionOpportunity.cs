using OpenActive.DatasetSite.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem
{
    /// <summary>
    /// These classes must be created by the booking system, the below are some simple examples.
    /// These should be created alongside the IdConfiguration and OpenDataFeeds settings, as the two work together
    /// 
    /// They can be completely customised to match the preferred ID structure of the booking system
    /// 
    /// There is a choice of `string`, `long?` and `Uri` available for each component of the ID
    /// </summary>
    public class SessionOpportunity : IBookableIdComponentsWithInheritance
    {
        public OpportunityType? OpportunityType { get; set; }
        public OpportunityType? OfferOpportunityType { get; set; }
        public long? SessionSeriesId { get; set; }
        public long? ScheduledSessionId { get; set; }
        public long? OfferId { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as SessionOpportunity;
            if (ReferenceEquals(other, null))
                return false;

            return OpportunityType == other.OpportunityType &&
                   OfferOpportunityType == other.OfferOpportunityType &&
                   SessionSeriesId == other.SessionSeriesId &&
                   ScheduledSessionId == other.ScheduledSessionId &&
                   OfferId == other.OfferId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = OpportunityType.GetHashCode();
                hashCode = (hashCode * 397) ^ OfferOpportunityType.GetHashCode();
                hashCode = (hashCode * 397) ^ SessionSeriesId.GetHashCode();
                hashCode = (hashCode * 397) ^ ScheduledSessionId.GetHashCode();
                hashCode = (hashCode * 397) ^ OfferId.GetHashCode();
                // ReSharper enable NonReadonlyMemberInGetHashCode
                return hashCode;
            }
        }

        public static bool operator ==(SessionOpportunity x, SessionOpportunity y) => x != null && x.Equals(y);

        public static bool operator !=(SessionOpportunity x, SessionOpportunity y) => !(x == y);
    }
}
