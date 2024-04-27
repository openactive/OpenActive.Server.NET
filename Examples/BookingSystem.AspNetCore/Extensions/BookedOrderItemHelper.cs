using System;
using System.Collections.Generic;
using OpenActive.FakeDatabase.NET;
using OpenActive.NET;
using OpenActive.Server.NET.StoreBooking;
using static OpenActive.FakeDatabase.NET.FakeDatabase;

namespace BookingSystem
{
    public static class BookedOrderItemHelper
    {
        public static void AddPropertiesToBookedOrderItem(IOrderItemContext ctx, BookedOrderItemInfo bookedOrderItemInfo)
        {
            // Setting the access code and access pass after booking.
            // If online session, add accessChannel
            if (bookedOrderItemInfo.AttendanceMode == AttendanceMode.Online || bookedOrderItemInfo.AttendanceMode == AttendanceMode.Mixed)
            {
                ctx.ResponseOrderItem.AccessChannel = new VirtualLocation()
                {
                    Name = "Zoom Video Chat",
                    Url = bookedOrderItemInfo.MeetingUrl,
                    AccessId = bookedOrderItemInfo.MeetingId,
                    AccessCode = bookedOrderItemInfo.MeetingPassword,
                    Description = "Please log into Zoom a few minutes before the event"
                };
            }

            // If offline session, add accessCode and accessPass
            if (bookedOrderItemInfo.AttendanceMode == AttendanceMode.Offline || bookedOrderItemInfo.AttendanceMode == AttendanceMode.Mixed)
            {
                ctx.ResponseOrderItem.AccessCode = new List<PropertyValue>
                                {
                                    new PropertyValue()
                                    {
                                        Name = "Pin Code",
                                        Description = bookedOrderItemInfo.PinCode
                                    }
                                };
                ctx.ResponseOrderItem.AccessPass = new List<ImageObject>
                                {
                                    new ImageObject()
                                    {
                                        Url = new Uri(bookedOrderItemInfo.ImageUrl)
                                    },
                                    new Barcode()
                                    {
                                        Url = new Uri(bookedOrderItemInfo.ImageUrl),
                                        Text = bookedOrderItemInfo.BarCodeText,
                                        CodeType = "code128"
                                    }
                                };
            }
        }

        public static void RemovePropertiesFromBookedOrderItem(IOrderItemContext ctx)
        {
            // Set RemainingAttendeeCapacity and MaximumAttendeeCapacity to null as the do not belong in the B and P responses.
            // For more information see: https://github.com/openactive/open-booking-api/issues/156#issuecomment-926643733
            ctx.ResponseOrderItem.OrderedItem.Object.RemainingAttendeeCapacity = null;
            ctx.ResponseOrderItem.OrderedItem.Object.MaximumAttendeeCapacity = null;
        }

    }
}
