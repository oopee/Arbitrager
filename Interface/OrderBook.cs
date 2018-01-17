using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{   
    public interface IOrderBook
    {
        AssetPair AssetPair { get; }
        List<OrderBookOrder> Bids { get; }
        List<OrderBookOrder> Asks { get; }
        PriceValue BestBuy { get; }
        PriceValue BestSell { get; }
    }

    public class OrderBook : IOrderBook
    {
        public AssetPair AssetPair { get; set; }
        public List<OrderBookOrder> Asks { get; set; } = new List<OrderBookOrder>();
        public List<OrderBookOrder> Bids { get; set; } = new List<OrderBookOrder>();

        public PriceValue BestBuy => Asks.FirstOrDefault()?.PricePerUnit ?? new PriceValue(0m, AssetPair.Quote);
        public PriceValue BestSell => Bids.FirstOrDefault()?.PricePerUnit ?? new PriceValue(0m, AssetPair.Quote);
    }

    public class OrderBookOrder
    {
        public PriceValue PricePerUnit { get; set; }
        public PriceValue VolumeUnits { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return string.Format("{0} x {1} (= {2})", PricePerUnit .ToStringWithAsset(), VolumeUnits.ToStringWithAsset(), (PricePerUnit * VolumeUnits.Value).ToStringWithAsset());
        }
    }
}
