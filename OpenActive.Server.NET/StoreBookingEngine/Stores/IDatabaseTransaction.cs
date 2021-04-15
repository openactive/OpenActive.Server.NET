using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.StoreBooking
{
    public interface IDatabaseTransaction : IDisposable { }
    public interface IDatabaseTransactionSync : IDatabaseTransaction
    {
        void Commit();
        void Rollback();
    }
    public interface IDatabaseTransactionAsync : IDatabaseTransaction
    {
        Task Commit();
        Task Rollback();
    }
}
