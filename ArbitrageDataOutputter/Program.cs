using System;
using System.Collections.Generic;

using Common;
using Interface;

using CommandLine;

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

    class CommonOptions
    {
        [Option('i', "interval", Default = 60, Required = false, HelpText = "Output interval in seconds")]
        public int Interval { get; set; }
    }

    [Verb("csv", HelpText = "Output to CSV file")]
    class CsvOptions : CommonOptions
    {
        [Option('o', "output", Default = @"arbitragedata.txt", Required = false, HelpText = "Outputfile for CSV export")]
        public string CsvOutputFile { get; set; }
    }

    [Verb("sheets", HelpText = "Output to Googlet Sheets document")]
    class SheetsOptions : CommonOptions
    {
    }

    class Program
    {
        static int Main(string[] args)
        {
            // If running with debugger, use project options to set proper command line arguments

            return CommandLine.Parser.Default.ParseArguments<CsvOptions, SheetsOptions>(args)
              .MapResult(
                (CsvOptions opts) => RunCsv(opts),
                (SheetsOptions opts) => RunGoogleSheets(opts),
                errs => HandleErrors(errs));
        }

        static int HandleErrors(IEnumerable<Error> errors)
        {
            return -1;
        }

        static int RunCsv(CsvOptions options)
        {
            var arbitrager = GetKrakenGdaxArbitrager();

            var source = new ArbitragerDataSource(arbitrager);
            var outputter = new CsvArbitrageDataOutputter(source, options.CsvOutputFile);

            RunOutputter(outputter, options);

            return 0;
        }

        static int RunGoogleSheets(SheetsOptions options)
        {
            return 0;
        }

        static void RunOutputter(IArbitrageDataOutputter outputter, CommonOptions options)
        {
            outputter.Interval = options.Interval;
            outputter.Start().Wait();

            Console.WriteLine($"Outputting data with {options.Interval} seconds interval, press 'q' to quit");

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
