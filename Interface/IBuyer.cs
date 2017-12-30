using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IBuyer : IExchange
    {
        Task<BalanceResult> GetCurrentBalance();
        Task<IAskOrderBook> GetAsks();
        Task<MyOrder> PlaceBuyOrder(decimal price, decimal volume);
    }

    public class BalanceResult
    {
        public Dictionary<string, decimal> All { get; set; }
        public decimal Eth { get; set; }
        public decimal Eur { get; set; }
    }

    public class MyOrder
    {
        public List<OrderId> Ids { get; set; } = new List<OrderId>();
        public decimal PricePerUnit { get; set; }
        public decimal Volume { get; set; }
        public decimal TotalPrice => PricePerUnit * Volume;

        public override string ToString()
        {
            return string.Format("({0}), {1}€ x {2} (= {3}€)", string.Join(", ", Ids), PricePerUnit, Volume, TotalPrice);
        }
    }

    public struct OrderId
    {
        public string Id { get; set; }

        public OrderId(string id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
