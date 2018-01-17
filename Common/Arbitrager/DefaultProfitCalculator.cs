using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface;

namespace Common
{
    public class DefaultProfitCalculator : Interface.IProfitCalculator
    {
        public ProfitCalculation CalculateProfit(ExchangeStatus buyer, ExchangeStatus seller, PriceValue quoteCurrencyLimit, PriceValue? baseCurrencyLimit = null)
        {
            var baseAsset = seller.OrderBook.AssetPair.Base;
            var quoteAsset = seller.OrderBook.AssetPair.Quote;

            // First check how much ETH can we buy for the cash limit.
            // We have to calculate this first in case the bids we have (top 1 or top 50)
            // do not cover the whole amount we are willing to buy from other exchange
            PriceValue baseCurrencyTotalBids = new PriceValue(seller.OrderBook.Bids.Select(x => x.VolumeUnits.Value).DefaultIfEmpty().Sum(), seller.OrderBook.AssetPair.Base);

            // Cap max amount of ETH to buy (if requested)
            if (baseCurrencyLimit != null)
            {                
                baseCurrencyTotalBids = PriceValue.Min(baseCurrencyLimit.Value, baseCurrencyTotalBids);
            }

            // Use all money until cash limit or available eth count is reached,
            // starting from the best ask
            PriceValue baseCurrencyCount = new PriceValue(0, baseAsset);
            PriceValue quoteCurrencySpent = new PriceValue(0, quoteAsset);
            PriceValue buyLimitPrice = new PriceValue(0, quoteAsset);
            PriceValue buyFee = new PriceValue(0, quoteAsset);
            int askNro = 0;
            while (quoteCurrencySpent < quoteCurrencyLimit && baseCurrencyCount < baseCurrencyTotalBids && buyer.OrderBook.Asks.Count > askNro)
            {
                var orders = buyer.OrderBook.Asks[askNro];

                var maxVolume = PriceValue.Min(baseCurrencyTotalBids - baseCurrencyCount, orders.VolumeUnits);
                var pricePerUnit = orders.PricePerUnit;
                var buyPricePerUnitWithFee = pricePerUnit.AddPercentage(buyer.TakerFee);
                var maxQuoteCurrencyToUseAtThisPrice = buyPricePerUnitWithFee * maxVolume.Value;

                var quoteCurrencyToUse = PriceValue.Min(quoteCurrencyLimit - quoteCurrencySpent, maxQuoteCurrencyToUseAtThisPrice);

                var buyAmount = new PriceValue((quoteCurrencyToUse / buyPricePerUnitWithFee).Value, baseAsset);
                var fee = (buyPricePerUnitWithFee - pricePerUnit) * buyAmount.Value;

                quoteCurrencySpent += quoteCurrencyToUse;
                baseCurrencyCount += buyAmount;
                buyLimitPrice = pricePerUnit; // we want to use last price as limit price
                buyFee += fee;

                ++askNro;
            }

            // How much can this ETH be sold for at other exchange
            PriceValue baseCurrencyLeftToSell = baseCurrencyCount;
            PriceValue baseCurrencySold = new PriceValue(0m, baseAsset);
            int bidNro = 0;
            PriceValue moneyEarned = new PriceValue(0m, quoteAsset);
            PriceValue sellFee = new PriceValue(0m, quoteAsset);
            while (baseCurrencyLeftToSell > 0 && seller.OrderBook.Bids.Count > bidNro)
            {
                var orders = seller.OrderBook.Bids[bidNro];
                var maxBaseCurrencyToSellAtThisPrice = orders.VolumeUnits;
                var baseCurrencyToSell = PriceValue.Min(baseCurrencyLeftToSell, maxBaseCurrencyToSellAtThisPrice);
                var pricePerUnit = orders.PricePerUnit;
                var sellPricePerUnitWithFee = pricePerUnit.SubtractPercentage(seller.TakerFee);
                var fee = (pricePerUnit - sellPricePerUnitWithFee) * baseCurrencyToSell.Value;

                moneyEarned += sellPricePerUnitWithFee * baseCurrencyToSell.Value;
                baseCurrencyLeftToSell -= baseCurrencyToSell;
                baseCurrencySold += baseCurrencyToSell;
                sellFee += fee;

                ++bidNro;
            }

            PriceValue profit = moneyEarned - quoteCurrencySpent;
            PriceValue profitAfterTax = profit.SubtractPercentage(PercentageValue.FromPercentage(30));

            return new ProfitCalculation()
            {
                QuoteCurrencySpent = quoteCurrencySpent,
                QuoteCurrencyEarned = moneyEarned,
                BaseCurrencyBuyCount = baseCurrencyCount,
                BaseCurrencySellCount = baseCurrencySold,
                Profit = profit,
                ProfitAfterTax = profitAfterTax,
                AllQuoteCurrencySpent = quoteCurrencySpent >= quoteCurrencyLimit,
                BuyFee = buyFee,
                SellFee = sellFee,
                BuyLimitPricePerUnit = buyLimitPrice
            };
        }

        public static IEnumerable<OrderBookOrder> GetFromOrderBook(AssetPair assetPair, IEnumerable<OrderBookOrder> orders, PriceValue? quoteCurrencyLimit, PriceValue? baseCurrencyLimit, decimal feePercentage = 0m)
        {
            var baseAsset = assetPair.Base;
            var quoteAsset = assetPair.Quote;

            PriceValue baseCurrencyCount = new PriceValue(0, baseAsset);
            PriceValue quoteCurrencySpent = new PriceValue(0, quoteAsset);

            foreach (var order in orders)
            {
                if (quoteCurrencyLimit != null && quoteCurrencySpent >= quoteCurrencyLimit)
                {
                    yield break;
                }

                if (baseCurrencyLimit != null && baseCurrencyCount >= baseCurrencyLimit)
                {
                    yield break;
                }

                PriceValue maxVolume = baseCurrencyLimit != null ? PriceValue.Min(baseCurrencyLimit.Value - baseCurrencyCount, order.VolumeUnits) : order.VolumeUnits;
                var buyPricePerUnitWithFee = order.PricePerUnit * (1m + feePercentage);
                var maxQuoteCurrencyToUseAtThisPrice = buyPricePerUnitWithFee * maxVolume.Value;

                var quoteCurrencyToUse = quoteCurrencyLimit != null ? PriceValue.Min(quoteCurrencyLimit.Value - quoteCurrencySpent, maxQuoteCurrencyToUseAtThisPrice) : maxQuoteCurrencyToUseAtThisPrice;

                PriceValue volume;
                if (quoteCurrencyToUse == maxQuoteCurrencyToUseAtThisPrice)
                {
                    volume = maxVolume;
                }
                else
                {
                    volume = new PriceValue((quoteCurrencyToUse / buyPricePerUnitWithFee).Value, baseAsset);
                }

                if (order.VolumeUnits != volume)
                {
                    baseCurrencyCount += volume;
                    yield return new OrderBookOrder()
                    {
                        PricePerUnit = order.PricePerUnit,
                        Timestamp = order.Timestamp,
                        VolumeUnits = volume
                    };
                }
                else
                {
                    baseCurrencyCount += order.VolumeUnits;
                    yield return order;
                }                
            }
        }
    }
}
