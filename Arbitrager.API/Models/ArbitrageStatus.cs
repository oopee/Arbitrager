using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.API.Models
{
    public class ArbitrageStatus
    {
        public List<ExchangeStatus> Exchanges { get; set; } = new List<ExchangeStatus>();

        public static ArbitrageStatus From(Interface.Status status)
        {
            return new ArbitrageStatus()
            {
                Exchanges = status.Exchanges.Select(e => ExchangeStatus.From(e)).ToList()
            };
        }
    }
}
