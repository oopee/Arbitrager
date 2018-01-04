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
        /// Profit / FiatSpent
        /// </summary>
        public decimal ProfitPercentage => FiatSpent > 0 ? Profit / FiatSpent : 0;

        /// <summary>
        /// FIAT earned by selling EthCount of ETH at exhange S.
        /// </summary>
        public decimal FiatEarned { get; set; }

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
