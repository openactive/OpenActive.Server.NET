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
    public class FacilityOpportunity : IBookableIdComponents
    {
        public OpportunityType? OpportunityType { get; set; }
        public long? FacilityUseId { get; set; }
        public long? SlotId { get; set; }
        public long? OfferId { get; set; }
        public long? IndividualFacilityUseId { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as FacilityOpportunity;
            if (ReferenceEquals(other, null))
                return false;

            return OpportunityType == other.OpportunityType &&
                   FacilityUseId == other.FacilityUseId &&
                   SlotId == other.SlotId &&
                   OfferId == other.OfferId &&
                   IndividualFacilityUseId == other.IndividualFacilityUseId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = OpportunityType.GetHashCode();
                hashCode = (hashCode * 397) ^ FacilityUseId.GetHashCode();
                hashCode = (hashCode * 397) ^ SlotId.GetHashCode();
                hashCode = (hashCode * 397) ^ OfferId.GetHashCode();
                hashCode = (hashCode * 397) ^ IndividualFacilityUseId.GetHashCode();
                // ReSharper enable NonReadonlyMemberInGetHashCode
                return hashCode;
            }
        }

        public static bool operator ==(FacilityOpportunity x, FacilityOpportunity y) => x != null && x.Equals(y);

        public static bool operator !=(FacilityOpportunity x, FacilityOpportunity y) => !(x == y);
    }
}
