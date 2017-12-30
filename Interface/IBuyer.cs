using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IBuyer : IUser
    {
        Task<BalanceResult> GetCurrentBalance();
        Task<IAskOrderBook> GetAsks();
        Task PlaceBuyOrder();
    }

    public class BalanceResult
    {
        public Dictionary<string, decimal> All { get; set; }
        public decimal Eth { get; set; }
        public decimal Eur { get; set; }
    }
}
