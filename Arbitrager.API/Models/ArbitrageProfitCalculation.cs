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
        /// QUOTE currency spent for buying BASE currency at exchange B (includes fees).
        /// </summary>
        public decimal QuoteCurrencySpent { get; set; }

        /// <summary>
        /// Amount of BASE currency that was bought (using QuoteCurrencySpent amount of quote currency) at exchange B.
        /// </summary>
        public decimal BaseCurrencyBuyCount { get; set; }

        /// <summary>
        /// Amount of BASE currency that could be sold at exchange S. This should be the max amount of BASE currency to arbitrage. 
        /// This is always less or equal than BaseCurrencyBuyCount.
        /// </summary>
        public decimal BaseCurrencySellCount { get; set; }

        /// <summary>
        /// Same as BaseCurrencySellCount (Amount of BASE currency that could be sold at exchange S. This should be the max amount of BASE currency to arbitrage. 
        /// This is always less or equal than BaseCurrencyBuyCount.)
        /// </summary>
        public decimal BaseCurrencyAmountToArbitrage => BaseCurrencySellCount;

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
        /// QUOTE currency earned by selling BaseCurrencySellCount of BASE currency at exhange S (includes fees).
        /// </summary>
        public decimal QuoteCurrencyEarned { get; set; }

        /// <summary>
        /// Amount of fees that have been added to QuoteCurrencySpent
        /// </summary>
        public decimal BuyFee { get; set; }

        /// <summary>
        /// Amount of fees that have been subtracted from QuoteCurrencyEarned
        /// </summary>
        public decimal SellFee { get; set; }

        /// <summary>
        /// A flag indicating if all incoming QUOTE currency could be used for buying BASE currency at exchange B (i.e. if there was enough volume to fill the order).
        /// </summary>
        public bool AllQuoteCurrencySpent { get; set; }

        public static ArbitrageProfitCalculation From(ProfitCalculation pc)
        {
            return new ArbitrageProfitCalculation()
            {
                AllQuoteCurrencySpent = pc.AllQuoteCurrencySpent,
                BuyFee = pc.BuyFee.Value,
                BuyLimitPricePerUnit = pc.BuyLimitPricePerUnit.Value,
                BaseCurrencyBuyCount = pc.BaseCurrencyBuyCount.Value,
                BaseCurrencySellCount = pc.BaseCurrencySellCount.Value,
                QuoteCurrencyEarned = pc.QuoteCurrencyEarned.Value,
                QuoteCurrencySpent = pc.QuoteCurrencySpent.Value,
                Profit = pc.Profit.Value,
                ProfitAfterTax = pc.ProfitAfterTax.Value,
                ProfitPercentage = pc.ProfitPercentage.Percentage,
                SellFee = pc.SellFee.Value,
            };
        }
    }
}
