using System;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace TransientFaultHandling
{
    public class SqlAzureTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        #region ITransientErrorDetectionStrategy implementation
        /// <summary>
        /// Determines whether the specified exception represents a transient failure that can be compensated by a retry.
        /// </summary>
        /// <param name="ex">The exception object to be verified.</param>
        /// <returns>True if the specified exception is considered as transient, otherwise false.</returns>
        public bool IsTransient(Exception ex)
        {
            // using the standard transient detection logic first
            var defaultDetectionStrategy = new SqlDatabaseTransientErrorDetectionStrategy();
            if (defaultDetectionStrategy.IsTransient(ex))
                return true;

            var sqlException = ex as SqlException;
            if (sqlException == null)
                return false;

            foreach (SqlError err in sqlException.Errors)
            {
                switch (err.Number)
                {
                    // sql connection timeout
                    case -2:
                        return true;
                    // transaction deadlock
                    case 1205:
                        return true;
                    // worker thread concurrency limit
                    case 10928:
                        return true;
                    // system is too busy
                    case 10929:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        #endregion
    }
}
