using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;

namespace Arbitrager.API.Models
{
    public class ArbitrageProfitCalculation
    {
        /// <summary>
        /// FIAT spent for buying ETH at exchange B (includes fees).
        /// </summary>
        public decimal FiatSpent { get; set; }

        /// <summary>
        /// Amount of ETH that was bought (using FiatSpent EUR) at exchange B.
        /// </summary>
        public decimal EthBuyCount { get; set; }

        /// <summary>
        /// Amount of ETH that could be sold at exchange S. This should be the max amount of ETH to arbitrage. This is always less or equal than EthBuyCount.
        /// </summary>
        public decimal EthSellCount { get; set; }

        /// <summary>
        /// Same as EthSellCount (Amount of ETH that could be sold at exchange S. This should be the max amount of ETH to arbitrage. This is always less or equal than EthBuyCount.)
        /// </summary>
        public decimal EthsToArbitrage => EthSellCount;

        /// <summary>
        /// The price of most expensive ask to fulfill this trade. This should be used as limit price when placing buy order.
        /// </summary>
        public decimal BuyLimitPricePerUnit { get; set; }

        /// <summary>
        /// Profit (i.e. FiatEarned - FiatSpent).
        /// </summary>
        public decimal Profit { get; set; }

        /// <summary>
        /// Profit after taxes (0.7*Profit).
        /// </summary>
        public decimal ProfitAfterTax { get; set; }

        /// <summary>
        /// Profit / FiatSpent
        /// </summary>
        public decimal ProfitPercentage { get; set; }

        /// <summary>
        /// FIAT earned by selling EthCount of ETH at exhange S (includes fees).
        /// </summary>
        public decimal FiatEarned { get; set; }

        /// <summary>
        /// Amount of fees that have been added to FiatSpent
        /// </summary>
        public decimal BuyFee { get; set; }

        /// <summary>
        /// Amount of fees that have been subtracted from FiatEarned
        /// </summary>
        public decimal SellFee { get; set; }

        /// <summary>
        /// A flag indicating if all incoming FIAT could be used for buying ETH at exchange B (i.e. if there was enough liquidity to fill the order).
        /// </summary>
        public bool AllFiatSpent { get; set; }

        public static ArbitrageProfitCalculation From(ProfitCalculation pc)
        {
            return new ArbitrageProfitCalculation()
            {
                AllFiatSpent = pc.AllFiatSpent,
                BuyFee = pc.BuyFee.Value,
                BuyLimitPricePerUnit = pc.BuyLimitPricePerUnit.Value,
                EthBuyCount = pc.EthBuyCount.Value,
                EthSellCount = pc.EthSellCount.Value,
                FiatEarned = pc.FiatEarned.Value,
                FiatSpent = pc.FiatSpent.Value,
                Profit = pc.Profit.Value,
                ProfitAfterTax = pc.ProfitAfterTax.Value,
                ProfitPercentage = pc.ProfitPercentage.Percentage,
                SellFee = pc.SellFee.Value,
            };
        }
    }
}
