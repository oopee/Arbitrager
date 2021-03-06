﻿using System;
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

        [Option('l', "fiatlimit", Default = 2000, Required = false, HelpText = "Amount of fiat to use for profit calculations")]
        public int FiatLimit { get; set; }

        [Option('s', "datasource", Default = "real", Required = false, HelpText = "Data source to use ('real' or 'fake'")]
        public string DataSource { get; set; }
    }

    [Verb("csv", HelpText = "Output to CSV file")]
    class CsvOptions : CommonOptions
    {
        [Option('o', "output", Default = @"arbitragedata.txt", Required = false, HelpText = "Outputfile for CSV export")]
        public string CsvOutputFile { get; set; }
    }

    [Verb("sheets", HelpText = "Output to Google Sheets document")]
    class SheetsOptions : CommonOptions
    {
        [Option("id", Required = true, HelpText = "Id for output spreadsheet")]
        public string SpreadsheetId { get; set; }
    }

    [Verb("slack", HelpText = "Output to Slack channel")]
    class SlackOptions : CommonOptions
    {
        [Option('h', "webhook", Required = true, HelpText = "Webhook for Slack channel integration")]
        public string WebhookURL { get; set; }
    }

    [Verb("sqlite", HelpText = "Output to Slack channel")]
    class SQLiteOptions : CommonOptions
    {
        [Option('f', "file", Required = true, HelpText = "File for SQLite database")]
        public string SQLiteFile { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            // If running with debugger, use project options to set proper command line arguments

            return CommandLine.Parser.Default.ParseArguments<CsvOptions, SheetsOptions, SlackOptions, SQLiteOptions>(args)
              .MapResult(
                (CsvOptions opts) => RunCsv(opts),
                (SheetsOptions opts) => RunGoogleSheets(opts),
                (SlackOptions opts) => RunSlack(opts),
                (SQLiteOptions opts) => RunSQLite(opts),
                errs => HandleErrors(errs));
        }

        static int HandleErrors(IEnumerable<Error> errors)
        {
            return 1;
        }

        static int RunCsv(CsvOptions options)
        {
            var source = GetDataSource(options);
            var outputter = new CsvArbitrageDataOutputter(source, options.CsvOutputFile);

            return RunOutputter(outputter, options);
        }

        static int RunGoogleSheets(SheetsOptions options)
        {
            var source = GetDataSource(options);
            var outputter = new GoogleSheetsArbitrageDataOutputter(source, options.SpreadsheetId);

            return RunOutputter(outputter, options);
        }

        static int RunSlack(SlackOptions options)
        {
            var source = GetDataSource(options);
            var outputter = new SlackAlertOutputter(source, options.WebhookURL);

            return RunOutputter(outputter, options);
        }

        static int RunSQLite(SQLiteOptions options)
        {
            var source = GetDataSource(options);
            var outputter = new SQLiteOutputter(source, options.SQLiteFile);

            return RunOutputter(outputter, options);
        }

        static int RunOutputter(IArbitrageDataOutputter outputter, CommonOptions options)
        {
            outputter.Interval = options.Interval;

            var shell = new Shell(outputter);

            try
            {
                shell.Run().Wait();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"EXCEPTION: {e.ToString()}");
                Console.ReadLine();

                return 1;
            }
        }

        static IArbitrageDataSource GetDataSource(CommonOptions options)
        {
            if (options.DataSource == "fake")
            {
                return new FakeDataSource();
            }
            else if (options.DataSource == "real")
            {
                var arbitrager = GetKrakenGdaxArbitrager();
                var source = new ArbitragerDataSource(arbitrager, options.FiatLimit);

                return source;
            }

            throw new NotSupportedException(options.DataSource);
        }

        static IArbitrager GetKrakenGdaxArbitrager()
        {
            return new Common.DefaultArbitrager(AssetPair.EthEur, new[] { GetKraken(), GetGdax() }, new DefaultProfitCalculator(), null, new DummyLogger());
        }

        static IExchange GetKraken()
        {
            var krakenConf = new Kraken.KrakenConfiguration()
            {
                Key = "",
                Secret = ""
            };

            return new Kraken.KrakenExchange(krakenConf, new DummyLogger());
        }

        static IExchange GetGdax()
        {
            var conf = new Gdax.GdaxConfiguration()
            {
                Key = "",
                Passphrase = "",
                Signature = ""
            };

            return new Gdax.GdaxExchange(conf, new DummyLogger(), isSandbox: false);
        }
    }
}
