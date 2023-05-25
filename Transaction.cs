using System.Transactions;


namespace DataAccess
{
    public class TransactionScopeEx
    {
        private static TransactionOptions transactionOptions = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TransactionManager.MaximumTimeout
        };

        /// <summary>
        /// Create a new TransactionScope object with the specified scope option, and aysnc flow option.
        /// </summary>
        /// <param name="flowOption">Option to control trnsaction flow across thread continuation (enabled/suppress)</param>
        /// <param name="scopeOption">Transaction-scope option (required/requires-new/suppress)</param>
        /// <returns></returns>
        public static TransactionScope Create(TransactionScopeAsyncFlowOption flowOption, TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            return new TransactionScope(scopeOption, transactionOptions, flowOption);
        }
    }
}
