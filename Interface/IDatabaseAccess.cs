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

        Task StoreTransaction(FullOrder transaction);
    }
}
