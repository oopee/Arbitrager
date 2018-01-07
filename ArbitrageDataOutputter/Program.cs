using System;

using Common;
using Interface;

namespace ArbitrageDataOutputter
{
    public class DummyLogger : LoggerBase
    {
        protected override LoggerBase Clone()
        {
            return new DummyLogger();
        }

        protected override void Log(LogLine logLine)
        {
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string outputFilePath = @"E:\arbitrageoutput.csv";
            var arbitrager = GetKrakenGdaxArbitrager();

            var source = new ArbitragerDataSource(arbitrager);

            var outputter = new CsvArbitrageDataOutputter(source, outputFilePath);
            outputter.Interval = 60; // 1 minute
            outputter.Start().Wait();

            Console.WriteLine("Outputting data, press 'q' to quit");
            
            while (Console.ReadKey(true).KeyChar != 'q')
            {
            }

            outputter.Stop();
        }

        static IArbitrager GetKrakenGdaxArbitrager()
        {
            return new Common.DefaultArbitrager((IBuyer)GetKraken(), (ISeller)GetGdax(), new DefaultProfitCalculator(), null, new DummyLogger());
        }

        static IExchange GetKraken()
        {
            var krakenConf = new Kraken.KrakenConfiguration()
            {
                Key = "",
                Secret = ""
            };

            return new Kraken.KrakenBuyer(krakenConf, new DummyLogger());
        }

        static IExchange GetGdax()
        {
            var conf = new Gdax.GdaxConfiguration()
            {
                Key = "",
                Passphrase = "",
                Signature = ""
            };

            return new Gdax.GdaxSeller(conf, new DummyLogger(), isSandbox: false);
        }
    }
}
