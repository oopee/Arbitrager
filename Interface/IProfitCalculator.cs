using System;
using System.Collections.Generic;
using System.Text;

namespace Interface
{
    public interface IProfitCalculator
    {
        ProfitCalculation CalculateProfit(BuyerStatus buyer, SellerStatus seller, decimal fiatLimit);
    }

    public class ProfitCalculation
    {
        /// <summary>
        /// FIAT spent for buying ETH at exchange B.
        /// </summary>
        public decimal FiatSpent { get; set; }

        /// <summary>
        /// Amount of ETH that was bought (using FiatSpent EUR) at exchange B.
        /// </summary>
        public decimal EthBuyCount { get; set; }

        /// <summary>
        /// Amount of ETH that could be sold at exchange S.
        /// </summary>
        public decimal EthSellCount { get; set; }

        /// <summary>
        /// Profit (i.e. FiatEarned - FiatSpent).
        /// </summary>
        public decimal Profit { get; set; }

        /// <summary>
        /// Profit after taxes (0.7*Profit).
        /// </summary>
        public decimal ProfitAfterTax { get; set; }

        /// <summary>
        /// FIAT earned by selling EthCount of ETH at exhange S.
        /// </summary>
        public decimal FiatEarned { get; set; }

        /// <summary>
        /// A flag indicating if all incoming FIAT could be used for buying ETH at exchange B (i.e. if there was enough liquidity to fill the order).
        /// </summary>
        public bool AllFiatSpent { get; set; }
    }
}
