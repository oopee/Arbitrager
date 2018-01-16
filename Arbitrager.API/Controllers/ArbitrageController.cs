using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Arbitrager.API.Controllers
{
    [Route("api/[controller]")]
    public class ArbitrageController : Controller
    {
        IArbitrager m_arbitrager;
        static IHubContext<Hubs.ArbitragerHub> s_hubContext;

        public ArbitrageController(IArbitrager arbitrager, IHubContext<Hubs.ArbitragerHub> hubContext)
        {
            m_arbitrager = arbitrager;
            s_hubContext = hubContext;
        }

        public static void ArbitragerStateChanged(object sender, ArbitrageContext e)
        {
            Console.WriteLine("State changed");
            var ctx = Models.ArbitrageContext.From(e);
            s_hubContext.Clients.All.InvokeAsync("StateChanged", new object[] { ctx });
        }

        [Route("status")]
        public async Task<Models.ArbitrageStatus> GetStatus()
        {
            var status = await m_arbitrager.GetStatus(true);
            return Models.ArbitrageStatus.From(status);
        }

        [Route("info")]
        public async Task<Models.ArbitrageInfo> GetInfo(decimal? amount = null)
        {
            BalanceOption fiatOption = amount == null ? BalanceOption.CapToBalance : BalanceOption.IgnoreBalance;
            var info = await m_arbitrager.GetInfoForArbitrage(PriceValue.FromEUR(amount ?? decimal.MaxValue), fiatOption, PriceValue.FromETH(decimal.MaxValue), BalanceOption.IgnoreBalance);
            return Models.ArbitrageInfo.From(info);
        }

        [Route("execute")]
        public async Task<Models.ArbitrageContext> GetExecute(decimal? amount = null)
        {
            var ctx = await m_arbitrager.Arbitrage(ArbitrageContext.Start(PriceValue.FromEUR(amount)));
            return Models.ArbitrageContext.From(ctx);
        }
    }
}
