using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.API.Models
{
    public class ArbitrageInfo
    {
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }

        public ExchangeStatus Buyer { get; set; }
        public ExchangeStatus Seller { get; set; }
        public ArbitrageProfitCalculation ProfitCalculation { get; set; }

        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public decimal MaxNegativeSpreadPercentage { get; set; }
        public decimal MaxNegativeSpread { get; set; }
        public decimal BaseCurrencyBalance { get; set; }
        public decimal QuoteCurrencyBalance { get; set; }

        public decimal MaxBaseCurrencyAmountToArbitrage { get; set; }
        public decimal MaxQuoteCurrencyAmountToSpend { get; set; }
        public decimal MaxQuoteCurrencyToEarn { get; set; }
        public decimal MaxQuoteCurrencyProfit { get; set; }
        public decimal MaxProfitPercentage { get; set; }
        public decimal MaxBuyFee { get; set; }
        public decimal MaxSellFee { get; set; }

        public decimal EstimatedAvgBuyUnitPrice { get; set; }
        public decimal EstimatedAvgSellUnitPrice { get; set; }
        public decimal EstimatedAvgNegativeSpread { get; set; }
        public decimal EstimatedAvgNegativeSpreadPercentage { get; set; }

        public decimal BestBuyPrice { get; set; }
        public decimal BestSellPrice { get; set; }

        public decimal BuyLimitPricePerUnit { get; set; }

        public bool IsBaseBalanceSufficient { get; set; }
        public bool IsQuoteBalanceSufficient { get; set; }
        public bool IsProfitable { get; set; }

        public static ArbitrageInfo From(Interface.ArbitrageInfo info)
        {
            return new Models.ArbitrageInfo()
            {
                BaseAsset = info.AssetPair.Base.Name,
                QuoteAsset = info.AssetPair.Quote.Name,
                BestBuyPrice = info.BestBuyPrice.Value,
                BestSellPrice = info.BestSellPrice.Value,
                BuyerName = info.BuyerName,
                MaxQuoteCurrencyProfit = info.MaxQuoteCurrencyProfit.Value,
                BuyLimitPricePerUnit = info.BuyLimitPricePerUnit.Value,
                EstimatedAvgBuyUnitPrice = info.EstimatedAvgBuyUnitPrice.Value,
                EstimatedAvgNegativeSpread = info.EstimatedAvgNegativeSpread.Value,
                EstimatedAvgNegativeSpreadPercentage = info.EstimatedAvgNegativeSpreadPercentage.Percentage,
                EstimatedAvgSellUnitPrice = info.EstimatedAvgSellUnitPrice.Value,
                BaseCurrencyBalance = info.BaseCurrencyBalance.Value,
                QuoteCurrencyBalance = info.QuoteCurrencyBalance.Value,
                IsBaseBalanceSufficient = info.IsBaseCurrencyBalanceSufficient,
                IsQuoteBalanceSufficient = info.IsQuoteCurrencyBalanceSufficient,
                IsProfitable = info.IsProfitable,
                MaxBuyFee = info.MaxBuyFee.Value,
                MaxBaseCurrencyAmountToArbitrage = info.MaxBaseCurrencyAmountToArbitrage.Value,
                MaxQuoteCurrencyToEarn = info.MaxQuoteCurrencyToEarn.Value,
                MaxQuoteCurrencyAmountToSpend = info.MaxQuoteCurrencyAmountToSpend.Value,
                MaxNegativeSpread = info.MaxNegativeSpread.Value,
                MaxNegativeSpreadPercentage = info.MaxNegativeSpreadPercentage.Percentage,
                MaxProfitPercentage = info.MaxProfitPercentage.Percentage,
                MaxSellFee = info.MaxSellFee.Value,
                ProfitCalculation = ArbitrageProfitCalculation.From(info.ProfitCalculation),
                SellerName = info.SellerName,
                Buyer = ExchangeStatus.From(info.Buyer),
                Seller = ExchangeStatus.From(info.Seller),
            };
        }
    }

    public class ExchangeStatus
    {
        public string Name { get; set; }
        public Balance Balance { get; set; }
        public OrderBook OrderBook { get; set; }

        public decimal TakerFee { get; set; }
        public decimal MakerFee { get; set; }

        public static ExchangeStatus From(Interface.ExchangeStatus status)
        {
            return new ExchangeStatus()
            {
                Balance = Balance.From(status.Balance),
                Name = status.Name,
                OrderBook = OrderBook.From(status.OrderBook),
            };
        }
    }

    public class Balance
    {
        public Dictionary<string, decimal> All { get; set; }
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public decimal Base { get; set; }
        public decimal Quote { get; set; }

        public static Balance From(Interface.BalanceResult balance)
        {
            return new Balance()
            {
                All = balance.All,
                Base = balance.BaseCurrency.Value,
                Quote = balance.QuoteCurrency.Value,
                BaseAsset = balance.AssetPair.Base.Name,
                QuoteAsset = balance.AssetPair.Quote.Name,
            };
        }
    }

    public class OrderBook
    {
        public List<OrderBookOrder> Asks { get; set; } = new List<OrderBookOrder>();
        public List<OrderBookOrder> Bids { get; set; } = new List<OrderBookOrder>();

        public static OrderBook From(Interface.IOrderBook book)
        {
            return new OrderBook()
            {
                Asks = book.Asks.Select(o => new OrderBookOrder() { PricePerUnit = o.PricePerUnit.Value, Timestamp = o.Timestamp, VolumeUnits = o.VolumeUnits.Value }).ToList(),
                Bids = book.Bids.Select(o => new OrderBookOrder() { PricePerUnit = o.PricePerUnit.Value, Timestamp = o.Timestamp, VolumeUnits = o.VolumeUnits.Value }).ToList(),
            };
        }
    }

    public class OrderBookOrder
    {
        public decimal PricePerUnit { get; set; }
        public decimal VolumeUnits { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DifferenceStatus
    {
        /// <summary>
        /// Absolute value of negative spead (negative spread -> ask is lower than bid). If spread is positive, then this value is zero.
        /// </summary>
        public decimal MaxNegativeSpread { get; set; }

        /// <summary>
        /// Ratio MaxNegativeSpread / LowestAskPrice.
        /// </summary>
        public decimal MaxNegativeSpreadPercentage { get; set; }
    }
}
