using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface ISeller : IUser
    {
        Task<BalanceResult> GetCurrentBalance();
        Task<IBidOrderBook> GetBids();
    }
}
