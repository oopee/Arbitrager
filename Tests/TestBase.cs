using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using Interface;
using Common;

namespace Tests
{
    public class TestBase
    {
        protected ILogger Logger = new TestLogger();
        protected IDatabaseAccess DataAccess = new DatabaseAccess.DatabaseAccess("testdb.sqlite");

        protected IExchange GetBinance(bool fake = false)
        {
            if (fake)
            {
                return new Binance.FakeBinanceExchange(Logger);
            }
            else
            {
                return new Binance.BinanceExchange(Binance.BinanceConfiguration.FromAppConfig(), Logger);
            }
        }

        protected IExchange GetKraken()
        {
            return new Kraken.KrakenExchange(Kraken.KrakenConfiguration.FromAppConfig(), Logger);
        }

        protected IExchange GetGdax()
        {
            return new Gdax.GdaxExchange(Gdax.GdaxConfiguration.FromAppConfig(), Logger, isSandbox: false);
        }

        protected IArbitrager GetKrakenGdaxArbitrager()
        {
            return new Common.DefaultArbitrager(AssetPair.EthEur, new[] { GetKraken(), GetGdax() }, new DefaultProfitCalculator(), DataAccess, Logger);
        }

        protected IExchange GetFakeBuyer()
        {
            return new Kraken.FakeKrakenExchange(Logger);
        }

        protected IExchange GetFakeSeller()
        {
            return new Gdax.FakeGdaxExchange(Logger);
        }

        protected IArbitrager GetFakeArbitrager()
        {
            return new Common.DefaultArbitrager(AssetPair.EthEur, new[] { GetFakeBuyer(), GetFakeSeller() }, new DefaultProfitCalculator(), DataAccess, Logger);
        }

        protected string GetDebugString(object obj)
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
        protected override LoggerBase Clone()
        {
            return new TestLogger();
        }

        protected override void Log(LogLine logLine)
        {
            System.Diagnostics.Debug.WriteLine(logLine.DefaultLine);
        }
    }
}
