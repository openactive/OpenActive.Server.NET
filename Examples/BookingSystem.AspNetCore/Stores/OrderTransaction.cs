using System.Threading.Tasks;
using OpenActive.FakeDatabase.NET;
using OpenActive.Server.NET.StoreBooking;

namespace BookingSystem
{
    public abstract class OrderTransaction : IDatabaseTransaction
    {
        public FakeDatabaseTransaction FakeDatabaseTransaction;

        public OrderTransaction()
        {
            FakeDatabaseTransaction = new FakeDatabaseTransaction(FakeBookingSystem.Database);
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

    public sealed class OrderTransactionSync : OrderTransaction, IDatabaseTransactionSync
    {
        public OrderTransactionSync()
        {
        }

        public void Commit()
        {
            FakeDatabaseTransaction.CommitTransaction();
        }

        public void Rollback()
        {
            FakeDatabaseTransaction.RollbackTransaction();
        }
    }

    public sealed class OrderTransactionAsync : OrderTransaction, IDatabaseTransactionAsync
    {
        public OrderTransactionAsync()
        {
        }

        public Task Commit()
        {
            return FakeDatabaseTransaction.CommitTransactionAsync();
        }

        public Task Rollback()
        {
            return FakeDatabaseTransaction.RollbackTransactionAsync();
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

