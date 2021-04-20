using System;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IDatabaseTransaction : IDisposable
    {
        ValueTask Commit(bool useAsync);
        ValueTask Rollback(bool useAsync);
    }
}
