using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IBidOrderBook
    {
        List<Order> Bids { get; }
    }

    public interface IAskOrderBook
    {
        List<Order> Asks { get; }
    }

    public class OrderBook : IBidOrderBook, IAskOrderBook
    {
        public List<Order> Asks { get; set; } = new List<Order>();
        public List<Order> Bids { get; set; } = new List<Order>();
    }

    public class Order
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
