using System;
using System.Collections.Generic;
using System.Text;

namespace Interface
{
    public interface IProfitCalculator
    {
        ProfitCalculation CalculateProfit(ExchangeStatus buyer, ExchangeStatus seller, PriceValue quoteCurrencyLimit, PriceValue? baseCurrencyLimit = null);
    }

    public class ProfitCalculation
    {
        /// <summary>
        /// QUOTE currency spent for buying BASE currency at exchange B (includes fees).
        /// </summary>
        public PriceValue QuoteCurrencySpent { get; set; }

        /// <summary>
        /// Amount of BASE currency that was bought (using QuoteCurrencySpent amount of quote currency) at exchange B.
        /// </summary>
        public PriceValue BaseCurrencyBuyCount { get; set; }

        /// <summary>
        /// Amount of BASE currency that could be sold at exchange S. This should be the max amount of BASE currency to arbitrage. 
        /// This is always less or equal than BaseCurrencyBuyCount.
        /// </summary>
        public PriceValue BaseCurrencySellCount { get; set; }

        /// <summary>
        /// Same as BaseCurrencySellCount (Amount of BASE currency that could be sold at exchange S. This should be the max amount of BASE currency to arbitrage. 
        /// This is always less or equal than BaseCurrencyBuyCount.)
        /// </summary>
        public PriceValue BaseCurrencyAmountToArbitrage => BaseCurrencySellCount;

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
        public PercentageValue ProfitPercentage => QuoteCurrencySpent > 0m ? PercentageValue.FromRatio((Profit / QuoteCurrencySpent).Value) : PercentageValue.Zero;

        /// <summary>
        /// QUOTE currency earned by selling BaseCurrencySellCount of BASE currency at exhange S (includes fees).
        /// </summary>
        public PriceValue QuoteCurrencyEarned { get; set; }

        /// <summary>
        /// Amount of fees that have been added to QuoteCurrencySpent
        /// </summary>
        public PriceValue BuyFee { get; set; }

        /// <summary>
        /// Amount of fees that have been subtracted from QuoteCurrencyEarned
        /// </summary>
        public PriceValue SellFee { get; set; }

        /// <summary>
        /// A flag indicating if all incoming QUOTE currency could be used for buying BASE currency at exchange B (i.e. if there was enough volume to fill the order).
        /// </summary>
        public bool AllQuoteCurrencySpent { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();

            string moneySpentSummary = string.Format("{0}", QuoteCurrencySpent.ToStringWithAsset());
            if (!AllQuoteCurrencySpent)
            {
                moneySpentSummary += " (capped, no more bids at other exchange)";
            }

            b.AppendLine("\t\tBuy limit price:\t\t{0:0.0000}", BuyLimitPricePerUnit.ToStringWithAsset());
            b.AppendLine("\t\tQUOTE limit:\t\t\t{0}", moneySpentSummary);
            b.AppendLine("\t\tBASE available to buy:\t\t{0}", BaseCurrencyBuyCount.ToStringWithAsset());
            b.AppendLine("\t\tBASE available to sell:\t\t{0}", BaseCurrencySellCount.ToStringWithAsset());
            b.AppendLine("\t\tBASE value at other exchange:\t{0}", QuoteCurrencyEarned.ToStringWithAsset());
            b.AppendLine("\t\tProfit:\t\t\t\t{0} ({1})", Profit.ToStringWithAsset(), PercentageValue.FromRatio((Profit / QuoteCurrencySpent).Value));
            b.AppendLine("\t\tProfit after tax:\t\t{}e ({1})", ProfitAfterTax.ToStringWithAsset(), PercentageValue.FromRatio((ProfitAfterTax / QuoteCurrencySpent).Value));

            return b.ToString();
        }
    }
}
