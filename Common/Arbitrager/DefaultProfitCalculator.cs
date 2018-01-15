using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface;

namespace Common
{
    public class DefaultProfitCalculator : Interface.IProfitCalculator
    {
        public ProfitCalculation CalculateProfit(ExchangeStatus buyer, ExchangeStatus seller, PriceValue fiatLimit, PriceValue? ethLimit = null)
        {
            // First check how much ETH can we buy for the cash limit.
            // We have to calculate this first in case the bids we have (top 1 or top 50)
            // do not cover the whole amount we are willing to buy from other exchange
            PriceValue ethTotalBids = seller.OrderBook.Bids.Select(x => x.VolumeUnits).DefaultIfEmpty().Sum().ToETH(); // TODO pricevalue

            // Cap max amount of ETH to buy (if requested)
            if (ethLimit != null)
            {                
                ethTotalBids = PriceValue.Min(ethLimit.Value, ethTotalBids);
            }

            // Use all money until cash limit or available eth count is reached,
            // starting from the best ask
            PriceValue ethCount = PriceValue.FromETH(0);
            PriceValue fiatSpent = PriceValue.FromEUR(0);
            PriceValue buyLimitPrice = default(PriceValue);
            PriceValue buyFee = PriceValue.FromEUR(0);
            int askNro = 0;
            while (fiatSpent < fiatLimit && ethCount < ethTotalBids && buyer.OrderBook.Asks.Count > askNro)
            {
                var orders = buyer.OrderBook.Asks[askNro];

                var maxVolume = Math.Min((ethTotalBids - ethCount).Value, orders.VolumeUnits); // TODO pricevalue
                var pricePerUnit = orders.PricePerUnit.ToEUR();
                var buyPricePerUnitWithFee = pricePerUnit.AddPercentage(buyer.TakerFee);
                var maxEursToUseAtThisPrice = buyPricePerUnitWithFee * maxVolume;

                var eursToUse = Math.Min(fiatLimit.Value - fiatSpent.Value, maxEursToUseAtThisPrice.Value).ToEUR();

                var buyAmount = (eursToUse / buyPricePerUnitWithFee).Value;
                var fee = (buyPricePerUnitWithFee - pricePerUnit) * buyAmount;

                fiatSpent += eursToUse;
                ethCount += buyAmount;
                buyLimitPrice = pricePerUnit; // we want to use last price as limit price
                buyFee += fee;

                ++askNro;
            }

            // How much can this ETH be sold for at other exchange
            PriceValue ethLeftToSell = ethCount;
            PriceValue ethSold = PriceValue.FromETH(0m);
            int bidNro = 0;
            PriceValue moneyEarned = PriceValue.FromEUR(0);
            PriceValue sellFee = PriceValue.FromEUR(0);
            while (ethLeftToSell > 0 && seller.OrderBook.Bids.Count > bidNro)
            {
                var orders = seller.OrderBook.Bids[bidNro];
                var maxEthToSellAtThisPrice = orders.VolumeUnits.ToETH();
                var ethToSell = Math.Min(ethLeftToSell.Value, maxEthToSellAtThisPrice.Value);
                var pricePerUnit = orders.PricePerUnit.ToEUR();
                var sellPricePerUnitWithFee = pricePerUnit.SubtractPercentage(seller.TakerFee);
                var fee = (pricePerUnit - sellPricePerUnitWithFee) * ethToSell;

                moneyEarned += sellPricePerUnitWithFee * ethToSell;
                ethLeftToSell -= ethToSell.ToETH();
                ethSold += ethToSell.ToETH();
                sellFee += fee;

                ++bidNro;
            }

            PriceValue profit = moneyEarned - fiatSpent;
            PriceValue profitAfterTax = profit.SubtractPercentage(PercentageValue.FromPercentage(30));

            return new ProfitCalculation()
            {
                FiatSpent = fiatSpent,
                FiatEarned = moneyEarned,
                EthBuyCount = ethCount,
                EthSellCount = ethSold,
                Profit = profit,
                ProfitAfterTax = profitAfterTax,
                AllFiatSpent = fiatSpent >= fiatLimit,
                BuyFee = buyFee,
                SellFee = sellFee,
                BuyLimitPricePerUnit = buyLimitPrice
            };
        }

        public static IEnumerable<OrderBookOrder> GetFromOrderBook(IEnumerable<OrderBookOrder> orders, decimal? fiatLimit, decimal? ethLimit, decimal feePercentage = 0m)
        {
            decimal ethCount = 0;
            decimal fiatSpent = 0;

            foreach (var order in orders)
            {
                if (fiatLimit != null && fiatSpent >= fiatLimit)
                {
                    yield break;
                }

                if (ethLimit != null && ethCount >= ethLimit)
                {
                    yield break;
                }

                decimal maxVolume = ethLimit != null ? Math.Min(ethLimit.Value - ethCount, order.VolumeUnits) : order.VolumeUnits;
                var buyPricePerUnitWithFee = order.PricePerUnit * (1m + feePercentage);
                var maxEursToUseAtThisPrice = buyPricePerUnitWithFee * maxVolume;

                var eursToUse = fiatLimit != null ? Math.Min(fiatLimit.Value - fiatSpent, maxEursToUseAtThisPrice) : maxEursToUseAtThisPrice;

                decimal volume;
                if (eursToUse == maxEursToUseAtThisPrice)
                {
                    volume = maxVolume;
                }
                else
                {
                    volume = eursToUse / buyPricePerUnitWithFee;
                }

                if (order.VolumeUnits != volume)
                {
                    ethCount += volume;
                    yield return new OrderBookOrder()
                    {
                        PricePerUnit = order.PricePerUnit,
                        Timestamp = order.Timestamp,
                        VolumeUnits = volume
                    };
                }
                else
                {
                    ethCount += order.VolumeUnits;
                    yield return order;
                }                
            }
        }
    }
}
