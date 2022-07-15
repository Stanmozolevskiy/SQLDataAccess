using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;


namespace DataAccess
{
    /// <summary>
    /// The DataAccess class defines static method to execute CRUD operations on a SQL Server database.
    /// </summary>
    public static partial class DataAccess
    {
        /// <summary>
        /// Execute a CRUD (Create/Read/Update/Delete) command asynchronously on the database using the specified query.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statement"></param>
        /// <param name="commandType"></param>
        /// <param name="parameters"></param>
        /// <returns>Query result as DataTable, wrapped in a Task</returns>
        public static async Task<DataTable> CRUDAsync(QueryContext context, string statement, CommandTypeEx commandType, params SqlParameter[] parameters) =>
           (await CRUDAsync<DataTable>(context, statement, commandType, parameters)).FirstOrDefault<DataTable>();
        /// <summary>
        /// Execute a CRUD (Create/Read/Update/Delete) command asynchronously on the database using the specified query.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statement"></param>
        /// <param name="commandType"></param>
        /// <param name="parameters"></param>
        /// <returns>Query result as JSON, wrapped in a Task</returns>
        public static async Task<string> CRUDAsyncJSON(QueryContext context, string statement, CommandTypeEx commandType, params SqlParameter[] parameters) =>
          (await CRUDAsync<string>(context, statement, commandType, parameters)).FirstOrDefault<string>();

        /// <summary>
        /// Execute a CRUD (Create/Read/Update/Delete) command asynchronously on the database using the specified query.
        /// </summary>
        /// <param name="context">Container of information for query execution</param>
        /// <param name="statement">SQL command text to execute or stored procedure name</param>
        /// <param name="commandType">Type of SQL command to execute</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Query result as List<T>, wrapped in a Task</returns>
        public static async Task<List<T>> CRUDAsync<T>(QueryContext context, string statement, CommandTypeEx commandType, params SqlParameter[] parameters)
        {
            // We use a TransactionScope to enclose the interaction with the database. The TransactionScope's default constructor sets
            // the transaction isolation level to Serializable and the timeout to 1 minute. Both of these settings are not really optimal.
            // The transaction timeout is unfortunate because the SqlCommand already has a CommandTimeout property (that defaults to 30 seconds).
            // Extending the command time out won't do any good if the transaction times out sooner!
            // We create a custom transaction-option and use that when the TransactionScope is constructed.
            for (int attempt = 0; attempt < context.MaxAttempts; System.Threading.Thread.Sleep(context.InterAttemptLatency))
            {
                try
                {
                    using (TransactionScope scope = TransactionScopeEx.Create(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        List<T> result = await CRUDHelperAsync<T>(context, statement, commandType, parameters).ConfigureAwait(false);
                        scope.Complete();
                        return result;
                    }
                }
                catch (SqlException ex)
                {
                    switch (ex.Number)
                    {
                        case SqlExceptionCode.DeadLock:
                        case SqlExceptionCode.Timeout:
                            if (++attempt == context.MaxAttempts)
                                throw;
                            break;
                        default:
                            throw;
                    }
                }
            }

            // The next statement is just to appease the compiler, without which it will complain that all code paths don't return a value.
            throw new InvalidOperationException("DataAccess.CRUD: Should never get here");
        }

        /// <summary>
        /// Execute a CRUD (Create/Read/Update/Delete) command asynchronously on the database using the specified query.
        /// </summary>
        /// <param name="context">Container of information for query execution</param>
        /// <param name="statement">SQL command text to execute or stored procedure name</param>
        /// <param name="commandType">Type of SQL command to execute</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Query result wrapped in a Task</returns>
        private static async Task<List<T>> CRUDHelperAsync<T>(QueryContext context, string statement, CommandTypeEx commandType, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(context.ConnectionString))
            using (SqlCommand command = new SqlCommand(statement, connection) { CommandTimeout = context.CommandTimeout })
            {
                command.Parameters.AddRange(parameters);
                try
                {
                    connection.Open();
                    return await CRUDHelperAsync<T>(context, command, (CommandType)commandType).ConfigureAwait(false);
                }
                finally
                {
                    command.Parameters.Clear();
                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }
        }

        /// <summary>
        /// Execute a CRUD (Create/Read/Update/Delete) command asynchronously on the database using the specified query.
        /// </summary>
        /// <param name="context">Container of information for query execution</param>
        /// <param name="command">SQL command to execute</param>
        /// <param name="commandType">Type of SQL command to execute</param>
        /// <returns>Query result wrapped in a Task</returns>
        private static async Task<List<T>> CRUDHelperAsync<T>(QueryContext context, SqlCommand command, CommandType commandType)
        {
            command.CommandType = commandType;
            Type expectedType = typeof(T);
            object result = null;

            if (expectedType == typeof(DataTable))
                result = await DataTableHelperAsync(command).ConfigureAwait(false);
            else if (expectedType == typeof(string))
                result = await DataJSONHelper(command).ConfigureAwait(false);
            else
                result = await DataTypeHelperAsync<T>(command).ConfigureAwait(false);

            return ((result == null) || (result == DBNull.Value)) ? default(List<T>) : ((IEnumerable)result).Cast<T>().ToList();
        }

        /// <summary>
        /// Execute a CRUD command asynchonously which is expected to return a List<T>.
        /// </summary>
        /// <param name="command">SQL command to execute</param>
        /// <returns>List<T> wrapped in a Task</returns>
        private static async Task<List<T>> DataTypeHelperAsync<T>(SqlCommand command)
        {
            DataTable result = new DataTable();
            await Task.Run(() => (new SqlDataAdapter(command)).Fill(result)).ConfigureAwait(false);
            return DataMapper.ConvertToList<T>(result);
        }

        /// <summary>
        /// Execute a CRUD command asynchonously which is expected to return a List<DataTable>.
        /// </summary>
        /// <param name="context">Container of information for query execution</param>
        /// <param name="command">SQL command to execute</param>
        /// <returns>List<DataTable> wrapped in a Task</returns>
        private static async Task<List<DataTable>> DataTableHelperAsync(SqlCommand command)
        {
            List<DataTable> response = new List<DataTable>();
            DataTable result = new DataTable();
            await Task.Run(() => (new SqlDataAdapter(command)).Fill(result)).ConfigureAwait(false);
            response.Add(result);
            return response;
        }
        /// <summary>
        /// Execute a CRUD command asynchonously which is expected to return a List<JSON>.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>List<JSON> wrapped in a Task</returns>
        private static async Task<List<string>> DataJSONHelper(SqlCommand command)
        {
            List<string> response = new List<string>();
            DataTable result = new DataTable();
            await Task.Run(() => (new SqlDataAdapter(command)).Fill(result)).ConfigureAwait(false);
            response.Add(JsonConvert.SerializeObject(result));
            return response;
        }
    }
}
