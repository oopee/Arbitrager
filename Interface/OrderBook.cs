using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IBidOrderBook
    {
        List<OrderBookOrder> Bids { get; }
    }

    public interface IAskOrderBook
    {
        List<OrderBookOrder> Asks { get; }
    }

    public class OrderBook : IBidOrderBook, IAskOrderBook
    {
        public List<OrderBookOrder> Asks { get; set; } = new List<OrderBookOrder>();
        public List<OrderBookOrder> Bids { get; set; } = new List<OrderBookOrder>();
    }

    public class OrderBookOrder
    {
        public decimal PricePerUnit { get; set; }
        public decimal VolumeUnits { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return string.Format("{0}€ x {1} (= {2}€)", PricePerUnit, VolumeUnits, PricePerUnit * VolumeUnits);
        }
    }
}
