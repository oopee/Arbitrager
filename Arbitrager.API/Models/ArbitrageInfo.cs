using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.API.Models
{
    public class ArbitrageInfo
    {
        public ExchangeStatus Buyer { get; set; }
        public ExchangeStatus Seller { get; set; }
        public ArbitrageProfitCalculation ProfitCalculation { get; set; }

        public string BuyerName { get; set; }
        public string SellerName { get; set; }
        public decimal MaxNegativeSpreadPercentage { get; set; }
        public decimal MaxNegativeSpreadEur { get; set; }
        public decimal EurBalance { get; set; }
        public decimal EthBalance { get; set; }

        public decimal MaxEthAmountToArbitrage { get; set; }
        public decimal MaxEursToSpend { get; set; }
        public decimal MaxEursToEarn { get; set; }
        public decimal MaxEurProfit { get; set; }
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

        public bool IsEurBalanceSufficient { get; set; }
        public bool IsEthBalanceSufficient { get; set; }
        public bool IsProfitable { get; set; }

        public static ArbitrageInfo From(Interface.ArbitrageInfo info)
        {
            return new Models.ArbitrageInfo()
            {
                BestBuyPrice = info.BestBuyPrice.Value,
                BestSellPrice = info.BestSellPrice.Value,
                BuyerName = info.BuyerName,
                MaxEurProfit = info.MaxEurProfit.Value,
                BuyLimitPricePerUnit = info.BuyLimitPricePerUnit.Value,
                EstimatedAvgBuyUnitPrice = info.EstimatedAvgBuyUnitPrice.Value,
                EstimatedAvgNegativeSpread = info.EstimatedAvgNegativeSpread.Value,
                EstimatedAvgNegativeSpreadPercentage = info.EstimatedAvgNegativeSpreadPercentage.Percentage,
                EstimatedAvgSellUnitPrice = info.EstimatedAvgSellUnitPrice.Value,
                EthBalance = info.EthBalance.Value,
                EurBalance = info.EurBalance.Value,
                IsEthBalanceSufficient = info.IsEthBalanceSufficient,
                IsEurBalanceSufficient = info.IsEurBalanceSufficient,
                IsProfitable = info.IsProfitable,
                MaxBuyFee = info.MaxBuyFee.Value,
                MaxEthAmountToArbitrage = info.MaxEthAmountToArbitrage.Value,
                MaxEursToEarn = info.MaxEursToEarn.Value,
                MaxEursToSpend = info.MaxEursToSpend.Value,
                MaxNegativeSpreadEur = info.MaxNegativeSpreadEur.Value,
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
        public decimal Eth { get; set; }
        public decimal Eur { get; set; }

        public static Balance From(Interface.BalanceResult balance)
        {
            return new Balance()
            {
                All = balance.All,
                Eth = balance.Eth.Value,
                Eur = balance.Eur.Value,
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
                Asks = book.Asks.Select(o => new OrderBookOrder() { PricePerUnit = o.PricePerUnit, Timestamp = o.Timestamp, VolumeUnits = o.VolumeUnits }).ToList(),
                Bids = book.Bids.Select(o => new OrderBookOrder() { PricePerUnit = o.PricePerUnit, Timestamp = o.Timestamp, VolumeUnits = o.VolumeUnits }).ToList(),
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
