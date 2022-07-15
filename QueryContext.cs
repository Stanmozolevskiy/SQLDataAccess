using System;
using System.Data;


namespace DataAccess
{
	public static partial class DataAccess
	{
		/// <summary>
		/// The QueryContext type is the container of context required while executing a SQL query.
		/// </summary>
		public class QueryContext
		{
			/// <summary>
			/// Initialize object state.
			/// </summary>
			/// <param name="connectionString">Database connection string</param>
			/// <param name="commandTimeout">SQL command time out (in seconds)</param>
			/// <param name="interAttemptLatency">The latency, in seconds, before re-trying a query</param>
			/// <param name="maxAttempts">The maximum number of time a query will be tried before giving up</param>
			/// <param name="validationMethod">Validation method to use on the result of executing a query</param>
			public QueryContext(string connectionString, int commandTimeout, TimeSpan interAttemptLatency, int maxAttempts)
			{
				ConnectionString = connectionString;
				CommandTimeout = commandTimeout;
				InterAttemptLatency = interAttemptLatency;
				MaxAttempts = maxAttempts;
			}

			/// <summary>
			/// SQL command time out (in seconds).
			/// </summary>
			public int CommandTimeout { get; private set; }

			/// <summary>
			/// Database connection string.
			/// </summary>
			public string ConnectionString { get; private set; }

			/// <summary>
			/// The latency, in seconds, before re-trying a query.
			/// </summary>
			public TimeSpan InterAttemptLatency { get; private set; }

			/// <summary>
			/// The maximum number of time a query will be tried before giving up.
			/// </summary>
			public int MaxAttempts { get; private set; }
		}
		private class SqlExceptionCode
		{
			internal const int DeadLock = 1205;
			internal const int Timeout = -2;
		}

		/// <summary>
		/// The CommandTypeEx "extends" the system defined CommandType, and adds the Function enumeration.
		/// </summary>
		public enum CommandTypeEx
		{
			StoredProcedure = CommandType.StoredProcedure,
			TableDirect = CommandType.TableDirect,
			Text = CommandType.Text,
			Function
		}
	}
}
