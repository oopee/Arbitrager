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

        protected IExchange GetKraken()
        {
            return new Kraken.KrakenBuyer(Kraken.KrakenConfiguration.FromAppConfig(), Logger);
        }

        protected IExchange GetGdax()
        {
            return new Gdax.GdaxSeller(Gdax.GdaxConfiguration.FromAppConfig(), Logger, isSandbox: false);
        }

        protected IArbitrager GetKrakenGdaxArbitrager()
        {
            return new Common.DefaultArbitrager((IBuyer)GetKraken(), (ISeller)GetGdax(), new DefaultProfitCalculator(), DataAccess, Logger);
        }

        protected IExchange GetFakeBuyer()
        {
            return new Kraken.FakeBuyer(Logger);
        }

        protected IExchange GetFakeSeller()
        {
            return new Gdax.FakeSeller(Logger);
        }

        protected IArbitrager GetFakeArbitrager()
        {
            return new Common.DefaultArbitrager((IBuyer)GetFakeBuyer(), (ISeller)GetFakeSeller(), new DefaultProfitCalculator(), DataAccess, Logger);
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
