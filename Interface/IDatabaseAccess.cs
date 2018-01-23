using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IDatabaseAccess
    {
        /// <summary>
        /// Removes the whole database and makes it ready for re-creation.
        /// </summary>
        Task ResetDatabase();

        /// <summary>
        /// Stores a given ArbitrageInfo state to database. 
        /// Updates existing if existingArbitrageId is given.
        /// Returns Id of the database arbitrage for future use.
        /// </summary>
        Task<int> StoreArbitrageInfo(int? existingArbitrageId, ArbitrageInfo info);

        Task StoreTransaction(FullOrder transaction);
    }
}
