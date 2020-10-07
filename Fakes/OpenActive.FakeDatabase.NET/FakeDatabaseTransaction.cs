﻿using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace OpenActive.FakeDatabase.NET
{
    public class FakeDatabaseTransaction : IDisposable
    {
        public IDbConnection DatabaseConnection;
        private IDbTransaction transaction;

        public FakeDatabaseTransaction(FakeDatabase database)
        {
            DatabaseConnection = database.Mem.Database.Open();
            transaction = DatabaseConnection.OpenTransaction(IsolationLevel.Serializable);
        }

        public void CommitTransaction()
        {
            transaction.Commit();
        }

        public void RollbackTransaction()
        {
            transaction.Rollback();
        }

        public void Dispose()
        {
            // Note dispose pattern of checking for null first,
            // to ensure Dispose() is not called twice
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }

            if (DatabaseConnection != null)
            {
                DatabaseConnection.Dispose();
                DatabaseConnection = null;
            }
        }
    }

}
