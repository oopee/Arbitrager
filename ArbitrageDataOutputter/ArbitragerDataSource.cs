using System;
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
                BestBid = info.BestSellPrice,
                BestAsk = info.BestBuyPrice,
                MaxNegativeSpreadEur = info.MaxNegativeSpreadEur,
                MaxNegativeSpreadPercentage = info.MaxNegativeSpreadPercentage,
                FiatLimit = FiatLimit,
                MaxProfitPercentage = info.MaxProfitPercentage,
                MaxProfitEur = info.MaxEurProfit
            };

            return dataPoint;
        }

        public async Task<ArbitrageInfo> GetSimulatedInfoForArbitrage(decimal fiatLimit)
        {
            // Get current prices, balances etc
            var status = await Arbitrager.GetStatus(false);
            
            // Calculate estimated profit based on prices/balances/etc
            var calc = ProfitCalculator.CalculateProfit(status.Buyer, status.Seller, fiatLimit);

            ArbitrageInfo info = new ArbitrageInfo()
            {
                Status = status,
                ProfitCalculation = calc,
                IsProfitable = calc.ProfitPercentage >= 0.02m // 2% threshold
            };

            return info;
        }
    }
}
