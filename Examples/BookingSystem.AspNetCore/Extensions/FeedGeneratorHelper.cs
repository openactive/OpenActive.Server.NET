using System;
using System.Collections.Generic;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.OpenBookingHelper;

namespace BookingSystem
{
    public static class FeedGeneratorHelper
    {
        public static List<OpenBookingFlowRequirement> OpenBookingFlowRequirement(bool requiresApproval, bool requiresAttendeeValidation, bool requiresAdditionalDetails, bool allowsProposalAmendment)
        {
            List<OpenBookingFlowRequirement> openBookingFlowRequirement = null;

            if (requiresApproval)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingApproval);
            }

            if (requiresAttendeeValidation)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingAttendeeDetails);
            }

            if (requiresAdditionalDetails)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingIntakeForm);
            }

            if (allowsProposalAmendment)
            {
                openBookingFlowRequirement = openBookingFlowRequirement ?? new List<OpenBookingFlowRequirement>();
                openBookingFlowRequirement.Add(OpenActive.NET.OpenBookingFlowRequirement.OpenBookingNegotiation);
            }
            return openBookingFlowRequirement;
        }

        public static EventAttendanceModeEnumeration MapAttendanceMode(AttendanceMode attendanceMode)
        {
            switch (attendanceMode)
            {
                case AttendanceMode.Offline:
                    return EventAttendanceModeEnumeration.OfflineEventAttendanceMode;
                case AttendanceMode.Online:
                    return EventAttendanceModeEnumeration.OnlineEventAttendanceMode;
                case AttendanceMode.Mixed:
                    return EventAttendanceModeEnumeration.MixedEventAttendanceMode;
                default:
                    throw new OpenBookingException(new OpenBookingError(), $"AttendanceMode Type {attendanceMode} not supported");
            }
        }
    }
}
