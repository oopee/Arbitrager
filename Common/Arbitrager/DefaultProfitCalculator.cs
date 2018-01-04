using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface;

namespace Common
{
    public class DefaultProfitCalculator : Interface.IProfitCalculator
    {
        public ProfitCalculation CalculateProfit(BuyerStatus buyer, SellerStatus seller, decimal fiatLimit, decimal? ethLimit = null)
        {
            // First check how much ETH can we buy for the cash limit.
            // We have to calculate this first in case the bids we have (top 1 or top 50)
            // do not cover the whole amount we are willing to buy from other exchange
            decimal ethTotalBids = seller.Bids.Bids.Select(x => x.VolumeUnits).DefaultIfEmpty().Sum();

            // Cap max amount of ETH to buy (if requested)
            if (ethLimit != null)
            {                
                ethTotalBids = Math.Min(ethLimit.Value, ethTotalBids);
            }

            // Use all money until cash limit or available eth count is reached,
            // starting from the best ask
            decimal ethCount = 0;
            decimal fiatSpent = 0;
            int askNro = 0;
            while (fiatSpent < fiatLimit && ethCount < ethTotalBids && buyer.Asks.Asks.Count > askNro)
            {
                var orders = buyer.Asks.Asks[askNro];

                var maxVolume = Math.Min(ethTotalBids - ethCount, orders.VolumeUnits);
                var buyPricePerUnitWithFee = orders.PricePerUnit * (1m + buyer.TakerFee);
                var maxEursToUseAtThisPrice = buyPricePerUnitWithFee * maxVolume;

                var eursToUse = Math.Min(fiatLimit - fiatSpent, maxEursToUseAtThisPrice);

                fiatSpent += eursToUse;
                ethCount += eursToUse / buyPricePerUnitWithFee;

                ++askNro;
            }

            // How much can this ETH be sold for at other exchange
            decimal ethLeftToSell = ethCount;
            decimal ethSold = 0m;
            int bidNro = 0;
            decimal moneyEarned = 0;
            while (ethLeftToSell > 0 && seller.Bids.Bids.Count > bidNro)
            {
                var orders = seller.Bids.Bids[bidNro];
                var maxEthToSellAtThisPrice = orders.VolumeUnits;
                var ethToSell = Math.Min(ethLeftToSell, maxEthToSellAtThisPrice);
                var sellPricePerUnitWithFee = orders.PricePerUnit * (1m - seller.TakerFee);

                moneyEarned += ethToSell * sellPricePerUnitWithFee;
                ethLeftToSell -= ethToSell;
                ethSold += ethToSell;

                ++bidNro;
            }

            decimal profit = moneyEarned - fiatSpent;
            decimal profitAfterTax = profit * 0.7m;

            return new ProfitCalculation()
            {
                FiatSpent = fiatSpent,
                FiatEarned = moneyEarned,
                EthBuyCount = ethCount,
                EthSellCount = ethSold,
                Profit = profit,
                ProfitAfterTax = profitAfterTax,
                AllFiatSpent = fiatSpent >= fiatLimit,
            };
        }
    }
}
