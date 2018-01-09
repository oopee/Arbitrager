using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using Microsoft.AspNetCore.Mvc;

namespace Arbitrager.API.Controllers
{
    [Route("api/[controller]")]
    public class ArbitrageController : Controller
    {
        IArbitrager m_arbitrager;

        public ArbitrageController(IArbitrager arbitrager)
        {
            m_arbitrager = arbitrager;
        }

        [Route("status")]
        public async Task<Status> GetStatus()
        {
            var status = await m_arbitrager.GetStatus(true);
            return status;
        }

        [Route("info")]
        public async Task<ArbitrageInfo> GetInfo(decimal? amount = null)
        {
            BalanceOption fiatOption = amount == null ? BalanceOption.CapToBalance : BalanceOption.IgnoreBalance;
            var info = await m_arbitrager.GetInfoForArbitrage(amount ?? decimal.MaxValue, fiatOption, decimal.MaxValue, BalanceOption.IgnoreBalance);
            return info;
        }

        [Route("execute")]
        public async Task<ArbitrageContext> PostExecute(decimal? amount = null)
        {
            var ctx = await m_arbitrager.Arbitrage(ArbitrageContext.Start(amount));
            return ctx;
        }
    }
}
