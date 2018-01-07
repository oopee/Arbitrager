using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Interface;

namespace ArbitrageDataOutputter
{
    public class ArbitragerDataSource : IArbitrageDataSource
    {
        IArbitrager Arbitrager { get; set; }

        public ArbitragerDataSource(IArbitrager arbitrager)
        {
            Arbitrager = arbitrager;
        }

        public async Task<ArbitrageDataPoint> GetCurrentData()
        {
            var status = await Arbitrager.GetStatus(false);
            var info = new ArbitrageInfo()
            {
                Status = status
            };

            var dataPoint = new ArbitrageDataPoint()
            {
                TimeStamp = DateTime.Now,
                BestBid = info.BestSellPrice,
                BestAsk = info.BestBuyPrice,
                MaxNegativeSpreadEur = info.MaxNegativeSpreadEur,
                MaxNegativeSpreadPercentage = info.MaxNegativeSpreadPercentage
            };

            return dataPoint;
        }
    }
}
