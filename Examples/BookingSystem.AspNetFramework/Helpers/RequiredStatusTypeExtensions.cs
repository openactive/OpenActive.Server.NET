using System;
using OpenActive.NET;

namespace BookingSystem.AspNetFramework.Helpers
{
    public static class RequiredStatusTypeExtensions
    {
        public static RequiredStatusType? Convert(this OpenActive.FakeDatabase.NET.RequiredStatusType? prepayment)
        {
            switch (prepayment)
            {
                case OpenActive.FakeDatabase.NET.RequiredStatusType.Required:
                    return RequiredStatusType.Required;
                case OpenActive.FakeDatabase.NET.RequiredStatusType.Optional:
                    return RequiredStatusType.Optional;
                case OpenActive.FakeDatabase.NET.RequiredStatusType.Unavailable:
                    return RequiredStatusType.Unavailable;
                case null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(prepayment), prepayment, null);
            }
        }
    }
}