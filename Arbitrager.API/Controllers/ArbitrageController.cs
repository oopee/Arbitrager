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
        IArbitrageManager m_manager;
        static IHubContext<Hubs.ArbitragerHub> s_hubContext;

        public ArbitrageController(IArbitrager arbitrager, IArbitrageManager arbitrageManager, IHubContext<Hubs.ArbitragerHub> hubContext)
        {
            m_arbitrager = arbitrager;
            m_manager = arbitrageManager;
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

        [Route("auto")]
        public async Task<bool> GetAuto(bool run)
        {
            if (run)
            {
                try
                {
                    m_manager.Run();
                }
                catch (InvalidOperationException e)
                {
                    // already running, which is ok
                }
            }

            m_manager.IsPaused = !run;
            return !m_manager.IsPaused;
        }
    }
}
