using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Interface;

namespace ArbitrageDataOutputter
{
    public class ArbitrageDataPoint
    {
        public DateTime TimeStamp { get; set; }

        public decimal BestBid { get; set; }
        public decimal BestAsk { get; set; }

        public decimal MaxNegativeSpreadPercentage { get; set; }
        public decimal MaxNegativeSpreadEur { get; set; }
    }

    public interface IArbitrageDataSource
    {
        Task<ArbitrageDataPoint> GetCurrentData();
    }
}
