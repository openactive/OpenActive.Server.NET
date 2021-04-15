using System.Threading.Tasks;
using OpenActive.FakeDatabase.NET;
using OpenActive.Server.NET.StoreBooking;

namespace BookingSystem
{
    public sealed class OrderTransaction : IDatabaseTransaction
    {
        public FakeDatabaseTransaction FakeDatabaseTransaction;

        public OrderTransaction()
        {
            FakeDatabaseTransaction = new FakeDatabaseTransaction(FakeBookingSystem.Database);
        }

        public void Commit()
        {
            FakeDatabaseTransaction.CommitTransaction();
        }

        public void Rollback()
        {
            FakeDatabaseTransaction.RollbackTransaction();
        }

        public void Dispose()
        {
            // Note dispose pattern of checking for null first,
            // to ensure Dispose() is not called twice
            if (FakeDatabaseTransaction != null)
            {
                FakeDatabaseTransaction.Dispose();
                FakeDatabaseTransaction = null;
            }
        }
    }

    public sealed class OrderTransactionSync : IDatabaseTransactionSync
    {
        public FakeDatabaseTransaction FakeDatabaseTransaction;

        public OrderTransactionSync()
        {
            FakeDatabaseTransaction = new FakeDatabaseTransaction(FakeBookingSystem.Database);
        }

        public void Commit()
        {
            FakeDatabaseTransaction.CommitTransaction();
        }

        public void Rollback()
        {
            FakeDatabaseTransaction.RollbackTransaction();
        }

        public void Dispose()
        {
            // Note dispose pattern of checking for null first,
            // to ensure Dispose() is not called twice
            if (FakeDatabaseTransaction != null)
            {
                FakeDatabaseTransaction.Dispose();
                FakeDatabaseTransaction = null;
            }
        }
    }

    public sealed class OrderTransactionAsync : IDatabaseTransactionAsync
    {
        public FakeDatabaseTransaction FakeDatabaseTransaction;

        public OrderTransactionAsync()
        {
            FakeDatabaseTransaction = new FakeDatabaseTransaction(FakeBookingSystem.Database);
        }

        public Task Commit()
        {
            return FakeDatabaseTransaction.CommitTransactionAsync();
        }

        public Task Rollback()
        {
            return FakeDatabaseTransaction.RollbackTransactionAsync();
        }

        public void Dispose()
        {
            // Note dispose pattern of checking for null first,
            // to ensure Dispose() is not called twice
            if (FakeDatabaseTransaction != null)
            {
                FakeDatabaseTransaction.Dispose();
                FakeDatabaseTransaction = null;
            }
        }
    }

    /*
    public sealed class EntityFrameworkOrderTransaction : IDatabaseTransaction
    {
        private OrderContext _context;
        private DbContextTransaction _dbContextTransaction;

        public EntityFrameworkOrderTransaction()
        {
            _context = new OrderContext();
            _dbContextTransaction = _context.Database.BeginTransaction();
        }

        public void Commit()
        {
            _context.SaveChanges();
            _dbContextTransaction.Commit();
        }

        public void Rollback()
        {
            _dbContextTransaction.Rollback();
        }

        public void Dispose()
        {
            // Note dispose pattern of checking for null first,
            // to ensure Dispose() is not called twice
            if (_dbContextTransaction != null)
            {
                _dbContextTransaction.Dispose();
                _dbContextTransaction = null;
            }

            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }
    */
}

