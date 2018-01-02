﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using Interface;
using Common;

namespace Tests
{
    [TestFixture]
    public class Runner
    {
        ILogger Logger = new TestLogger();

        [Explicit]
        [Test]
        public async Task Kraken_GetOpenOrders()
        {
            var openOrders = await GetKraken().GetOpenOrders();
            Logger.Info(GetDebugString(openOrders));
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetStatus()
        {
            var result = await GetKrakenGdaxArbitrager().GetStatus(false);
            Logger.Info(result.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetStatus_WithBalance()
        {
            var result = await GetKrakenGdaxArbitrager().GetStatus(true);
            Logger.Info(result.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetAccounts()
        {
            var result = await GetKrakenGdaxArbitrager().GetAccountsInfo();
            Logger.Info(result.ToString());
        }

        IExchange GetKraken()
        {
            return new Kraken.KrakenBuyer(Kraken.KrakenConfiguration.FromAppConfig(), Logger);
        }

        IExchange GetGdax()
        {
            return new Gdax.GdaxSeller(Gdax.GdaxConfiguration.FromAppConfig(), Logger, isSandbox: false);
        }

        IArbitrager GetKrakenGdaxArbitrager()
        {
            return new Common.DefaultArbitrager((IBuyer)GetKraken(), (ISeller)GetGdax(), Logger);
        }

        string GetDebugString(object obj)
        {
            if (obj == null)
            {
                return "(null)";
            }

            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings()
            {

            });

            return jsonString;
        }
    }

    public class TestLogger : Common.LoggerBase
    {
        protected override void Log(LogLine logLine)
        {
            System.Diagnostics.Debug.WriteLine(logLine.DefaultLine);
        }
    }
}
