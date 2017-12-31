using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IExchange
    {
        string Name { get; }

        Task<BalanceResult> GetCurrentBalance();

        Task<List<FullMyOrder>> GetOpenOrders();
        Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null);
    }

    public class GetOrderArgs
    {
        public DateTime? StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
    }
}
