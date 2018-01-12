using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrageDataOutputter
{
    public class FakeDataSource : IArbitrageDataSource
    {
        public Task<ArbitrageDataPoint> GetCurrentData()
        {
            var r = new Random();

            var dataPoint = new ArbitrageDataPoint()
            {
                TimeStamp = DateTime.Now,
                BestAsk = r.Next(900, 970),
                BestBid = r.Next(960, 1050),
                FiatLimit = 2000
            };

            dataPoint.MaxNegativeSpreadEur = dataPoint.BestBid - dataPoint.BestAsk;
            dataPoint.MaxNegativeSpreadPercentage = dataPoint.MaxNegativeSpreadEur / dataPoint.BestAsk * 100;

            dataPoint.MaxProfitEur = dataPoint.FiatLimit * dataPoint.MaxNegativeSpreadPercentage / 100;
            dataPoint.MaxProfitPercentage = dataPoint.MaxNegativeSpreadPercentage;

            return Task.FromResult(dataPoint);
        }
    }
}
