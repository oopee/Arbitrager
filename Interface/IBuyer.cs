using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public class BalanceResult
    {
        public AssetPair AssetPair { get; set; }
        public Dictionary<string, decimal> All { get; set; } = new Dictionary<string, decimal>();
        public PriceValue BaseCurrency { get; set; }
        public PriceValue QuoteCurrency { get; set; }
    }

    public class MinimalOrder
    {
        public OrderId Id { get; set; }
        public OrderSide Side { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Side, Id);
        }
    }

    public enum OrderSide
    {
        Unknown = 0,
        Buy = 1,
        Sell = 2
    }

    public enum OrderType
    {
        Unknown = 0,
        Limit = 1,
        Market = 2,
        Other = 999
    }

    public enum OrderState
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
        Cancelled = 3
    }

    public class FullOrder
    {
        /// <summary>
        /// Name of the Source Bank or Exchange. Possibly even a wallet Id.
        /// </summary>
        public string SourceExchange { get; set; }

        /// <summary>
        /// Name of the Target Bank or Exchange. Possibly even a wallet Id.
        /// </summary>
        public string TargetExchange { get; set; }

        /// <summary>
        /// Source Asset code this transaction transfers assets from. 
        /// </summary>
        public Asset SourceAsset { get; set; }

        /// <summary>
        /// Target Asset code this transaction transfers assets to.
        /// </summary>
        public Asset TargetAsset { get; set; }

        /// <summary>
        /// Base asset/currency. The base currency represents how much of the quote currency is needed for you to get one unit of the base currency.
        /// </summary>
        public Asset BaseAsset { get; set; }

        /// <summary>
        /// Quote asset/currency. See base currency.
        /// </summary>
        public Asset QuoteAsset { get; set; }

        /// <summary>
        /// Unique identifier
        /// </summary>
        public OrderId Id { get; set; }

        /// <summary>
        /// BUY or SELL.
        /// </summary>
        public OrderSide Side { get; set; }

        /// <summary>
        /// OPEN, CLOSED or CANCELLED.
        /// </summary>
        public OrderState State { get; set; }

        /// <summary>
        /// MARKET or LIMIT.
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// The total volume of BASE currency to buy/sell.
        /// </summary>
        public PriceValue Volume { get; set; }

        /// <summary>
        /// The actual/executed value of BASE currency that was bought/sold. The final value of this property is determined after
        /// the order has been closed/cancelled.
        /// </summary>
        public PriceValue FilledVolume { get; set; }

        /// <summary>
        /// The limit price (in QUOTE currency) used in this order. This is applicable only if order type is 'Limit'.
        /// </summary>
        public PriceValue? LimitPrice { get; set; }

        /// <summary>
        /// A fee in QUOTE currency. The fee may be set only after order has been closed/cancelled. Note that for buy orders the fee increases the total cost
        /// (i.e. causes more quote currency to be spent) and for sell orders the fee decreases the total cost (i.e. causes less quote currency to be earned).
        /// </summary>
        public PriceValue Fee { get; set; }

        /// <summary>
        /// A cost of this order in QUOTE currency WITHOUT FEES.
        /// </summary>
        public PriceValue CostExcludingFee { get; set; }

        /// <summary>
        /// A cost of this order in QUOTE currency WITH FEES. Note that for buy orders this is greater or equal to CostExcludingFee 
        /// and for sell this is less or equal to CostExcludingFee.
        /// </summary>
        public PriceValue CostIncludingFee => CostExcludingFee + (Side == OrderSide.Buy ? Fee : -Fee);

        /// <summary>
        /// Timestamp when this order was created and opened.
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Timestamp when this order was closed/cancelled.
        /// </summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>
        /// Timestamp when this order expires automatically.
        /// </summary>
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// Average unit price, basically CostIncludingFee / FilledVolume.
        /// </summary>
        public PriceValue AverageUnitPrice => FilledVolume.Value == 0 ? new PriceValue(0m, FilledVolume.Asset) : CostIncludingFee / FilledVolume.Value;

        public FullOrder()
        {
        }

        public override string ToString()
        {
            switch (Type)
            {
                default:
                case OrderType.Unknown:
                case OrderType.Market:
                case OrderType.Other:
                    return string.Format("{0} {1} ({2}), {6} {3} for {4} (avg. unit price {5}).", State, Side, Id, CostIncludingFee.ToStringWithAsset(), FilledVolume.ToStringWithAsset(), AverageUnitPrice.ToStringWithAsset(), Type.ToString().ToUpper());
                case OrderType.Limit:
                    return string.Format("{0} {1} ({2}), LIMIT {3} x {4} (= {5}). Filled: {6} for {7} (avg. unit price {8})", State, Side, Id, LimitPrice?.ToStringWithAsset(), Volume, (LimitPrice * Volume.Value)?.ToStringWithAsset(), CostIncludingFee.ToStringWithAsset(), FilledVolume.ToStringWithAsset(), AverageUnitPrice.ToStringWithAsset());
            }            
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

        public static bool operator==(OrderId a, OrderId b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            return a.Id == b.Id;
        }

        public static bool operator!=(OrderId a, OrderId b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(OrderId))
            {
                return false;
            }

            OrderId o = (OrderId)obj;
            return this == o;
        }

        public static implicit operator string(OrderId id)
        {
            return id.Id;
        }
    }

    public struct PaymentMethodId
    {
        public string Id { get; set; }

        public PaymentMethodId(string id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
