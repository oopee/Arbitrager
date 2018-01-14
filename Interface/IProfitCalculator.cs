using System;
using System.Collections.Generic;
using System.Text;

namespace Interface
{
    public interface IProfitCalculator
    {
        ProfitCalculation CalculateProfit(BuyerStatus buyer, SellerStatus seller, PriceValue fiatLimit, PriceValue ethLimit = default(PriceValue));
    }

    public class ProfitCalculation
    {
        /// <summary>
        /// FIAT spent for buying ETH at exchange B (includes fees).
        /// </summary>
        public PriceValue FiatSpent { get; set; }

        /// <summary>
        /// Amount of ETH that was bought (using FiatSpent EUR) at exchange B.
        /// </summary>
        public PriceValue EthBuyCount { get; set; }

        /// <summary>
        /// Amount of ETH that could be sold at exchange S. This should be the max amount of ETH to arbitrage. This is always less or equal than EthBuyCount.
        /// </summary>
        public PriceValue EthSellCount { get; set; }

        /// <summary>
        /// Same as EthSellCount (Amount of ETH that could be sold at exchange S. This should be the max amount of ETH to arbitrage. This is always less or equal than EthBuyCount.)
        /// </summary>
        public PriceValue EthsToArbitrage => EthSellCount;

        /// <summary>
        /// The price of most expensive ask to fulfill this trade. This should be used as limit price when placing buy order.
        /// </summary>
        public PriceValue BuyLimitPricePerUnit { get; set; }

        /// <summary>
        /// Profit (i.e. FiatEarned - FiatSpent).
        /// </summary>
        public PriceValue Profit { get; set; }

        /// <summary>
        /// Profit after taxes (0.7*Profit).
        /// </summary>
        public PriceValue ProfitAfterTax { get; set; }

        /// <summary>
        /// Profit / FiatSpent
        /// </summary>
        public PercentageValue ProfitPercentage => FiatSpent > 0m ? (Profit / FiatSpent).Value : 0m;

        /// <summary>
        /// FIAT earned by selling EthCount of ETH at exhange S (includes fees).
        /// </summary>
        public PriceValue FiatEarned { get; set; }

        /// <summary>
        /// Amount of fees that have been added to FiatSpent
        /// </summary>
        public PriceValue BuyFee { get; set; }

        /// <summary>
        /// Amount of fees that have been subtracted from FiatEarned
        /// </summary>
        public PriceValue SellFee { get; set; }

        /// <summary>
        /// A flag indicating if all incoming FIAT could be used for buying ETH at exchange B (i.e. if there was enough liquidity to fill the order).
        /// </summary>
        public bool AllFiatSpent { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();

            string moneySpentSummary = string.Format("{0:0.00}e", FiatSpent);
            if (!AllFiatSpent)
            {
                moneySpentSummary += " (capped, no more bids at other exchange)";
            }

            b.AppendLine("\t\tBuy limit price:\t\t{0:0.0000}", BuyLimitPricePerUnit);
            b.AppendLine("\t\tCash limit:\t\t\t{0}", moneySpentSummary);
            b.AppendLine("\t\tETH available to buy:\t\t{0:0.0000}", EthBuyCount);
            b.AppendLine("\t\tETH available to sell:\t\t{0:0.0000}", EthSellCount);
            b.AppendLine("\t\tETH value at other exchange:\t{0:0.00}e", FiatEarned);
            b.AppendLine("\t\tProfit:\t\t\t\t{0:0.00}e ({1:0.00}%)", Profit, Profit / FiatSpent * 100.0m);
            b.AppendLine("\t\tProfit after tax:\t\t{0:0.00}e ({1:0.00}%)", ProfitAfterTax, ProfitAfterTax / FiatSpent * 100.0m);

            return b.ToString();
        }
    }
}
