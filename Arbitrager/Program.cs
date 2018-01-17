using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;
using Kraken;
using Gdax;
using System.Configuration;
using Common;

namespace Arbitrager
{
    class Program
    {
        static void Main(string[] args)
        {
            //
            var now = DateTime.Now;
            // var logger = new Common.ConsoleAndFileLogger(string.Format("arbitrager_{0}_{1}_{2}_{3}_{4}_{5}.log", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second));
            var logger = new Common.ConsoleLogger();
            Interface.Logger.StaticLogger = logger;
            //

            var configuration = Utils.AppConfigLoader.Instance.AppSettings("configuration");

            var dataAccess = new DatabaseAccess.DatabaseAccess(configuration);
            dataAccess.ResetDatabase().Wait();

            IExchange buyer;
            IExchange seller;

            AssetPair assetPair = new AssetPair(Asset.ETH, Asset.EUR);
            // AssetPair assetPair = new AssetPair(Asset.NEO, Asset.USDT);

            switch (configuration)
            {
                case "fake":
                    buyer = new FakeKrakenExchange(Logger.StaticLogger) { BalanceBase = new PriceValue(5m, assetPair.Base), BalanceQuote = new PriceValue(2000m, assetPair.Quote) };
                    seller = new FakeGdaxExchange(Logger.StaticLogger) { BalanceBase = new PriceValue(5m, assetPair.Base), BalanceQuote = new PriceValue(1000m, assetPair.Quote) };
                    break;
                case "simulated":
                    buyer = new SimulatedKrakenExchange(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger) { BalanceBase = new PriceValue(5m, assetPair.Base), BalanceQuote = new PriceValue(5000m, assetPair.Quote) };
                    seller = new SimulatedGdaxExchange(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false) { BalanceBase = new PriceValue(5m, assetPair.Base), BalanceQuote = new PriceValue(5000m, assetPair.Quote) };
                    break;
                case "real":
                    buyer = new KrakenExchange(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger);
                    seller = new GdaxExchange(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false);
                    break;
                default:
                    throw new ArgumentException("Invalid configuration (see App.config). Valid values are: simulated, real");
            }

            logger.Info("Configuration: {0}", configuration);
            logger.Info("IBuyer       : {0}", buyer.GetType().Name);
            logger.Info("ISeller      : {0}", seller.GetType().Name);

            var appType = Utils.AppConfigLoader.Instance.AppSettings("consoleArbitragerAppType") ?? "manual";

            if (appType == "manual")
            {
                var shell = new Shell(
                    assetPair,
                    new ConsoleArbitrager(assetPair, buyer, seller, new Common.DefaultProfitCalculator(), dataAccess, Logger.StaticLogger),
                    dataAccess,
                    Logger.StaticLogger);

                try
                {
                    shell.Run().Wait();
                }
                catch (Exception e)
                {
                    logger.Error("EXCEPTION: {0}", e);
                    Console.ReadLine();
                }
            }
            else if (appType == "auto")
            {
                var manager = new Common.ArbitrageManager.DefaultArbitrageManager(
                    new DefaultArbitrager(
                        assetPair, 
                        new[] { buyer, seller }, 
                        new Common.DefaultProfitCalculator(), dataAccess, Logger.StaticLogger),                    
                    Logger.StaticLogger,
                    TimeService.Clock);

                manager.Run().Wait();
            }
            else
            {
                throw new ArgumentException("Invalid consoleArbitragerAppType (see App.config). Valid values are: manual, auto");
            }
        }
    }   

}
