using System;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IDatabaseTransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }
}
