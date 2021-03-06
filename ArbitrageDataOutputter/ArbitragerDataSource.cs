﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Common;
using Interface;

namespace ArbitrageDataOutputter
{
    public class ArbitragerDataSource : IArbitrageDataSource
    {
        IArbitrager Arbitrager { get; set; }
        IProfitCalculator ProfitCalculator { get; set; }
        decimal FiatLimit { get; set; }

        public ArbitragerDataSource(IArbitrager arbitrager, decimal fiatLimit)
        {
            Arbitrager = arbitrager;
            ProfitCalculator = new DefaultProfitCalculator();
            FiatLimit = fiatLimit;
        }

        public async Task<ArbitrageDataPoint> GetCurrentData()
        {
            var info = await GetSimulatedInfoForArbitrage(FiatLimit);

            var dataPoint = new ArbitrageDataPoint()
            {
                TimeStamp = DateTime.Now,
                BestBid = info.BestSellPrice.Value,
                BestAsk = info.BestBuyPrice.Value,
                MaxNegativeSpreadEur = info.MaxNegativeSpread.Value,
                MaxNegativeSpreadPercentage = info.MaxNegativeSpreadPercentage.Ratio,
                FiatLimit = FiatLimit,
                MaxProfitPercentage = info.MaxProfitPercentage.Ratio,
                MaxProfitEur = info.MaxQuoteCurrencyProfit.Value
            };

            return dataPoint;
        }

        public async Task<ArbitrageInfo> GetSimulatedInfoForArbitrage(decimal fiatLimit)
        {
            // Get current prices, balances etc
            var status = await Arbitrager.GetStatus(false);
            var buyer = status.Exchanges[0];
            var seller = status.Exchanges[1];
            
            // Calculate estimated profit based on prices/balances/etc
            var calc = ProfitCalculator.CalculateProfit(buyer, seller, PriceValue.FromEUR(fiatLimit));

            ArbitrageInfo info = new ArbitrageInfo()
            {
                AssetPair = AssetPair.EthEur,
                Buyer = buyer,
                Seller = seller,
                ProfitCalculation = calc,
                IsProfitable = calc.ProfitPercentage >= PercentageValue.FromPercentage(2m) // 2% threshold
            };

            return info;
        }
    }
}
