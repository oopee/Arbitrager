using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface ISeller : IExchange
    {
        Task<BalanceResult> GetCurrentBalance();
        Task<IBidOrderBook> GetBids();
        Task<MyOrder> PlaceSellOrder(decimal price, decimal volume);
    }
}
