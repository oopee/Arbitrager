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
            var logger = new Common.ConsoleLogger();
            Interface.Logger.StaticLogger = logger;
            //

            var configuration = Utils.AppConfigLoader.Instance.AppSettings("configuration");

            var dataAccess = new DatabaseAccess.DatabaseAccess(configuration);
            dataAccess.ResetDatabase().Wait();

            IBuyer buyer;
            ISeller seller;

            switch (configuration)
            {
                case "fake":
                    buyer = new FakeBuyer(Logger.StaticLogger) { BalanceEth = 0m, BalanceEur = 2000m };
                    seller = new FakeSeller(Logger.StaticLogger) { BalanceEth = 1m, BalanceEur = 0m };
                    break;
                case "simulated":
                    buyer = new SimulatedKrakenBuyer(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger) { BalanceEth = 0m, BalanceEur = 2000m };
                    seller = new SimulatedGdaxSeller(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false) { BalanceEth = 1m, BalanceEur = 0m };
                    break;
                case "real":
                    buyer = new KrakenBuyer(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger);
                    seller = new GdaxSeller(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false);
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
                    new ConsoleArbitrager(buyer, seller, new Common.DefaultProfitCalculator(), dataAccess, Logger.StaticLogger),
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
                    new DefaultArbitrager(buyer, seller, new Common.DefaultProfitCalculator(), dataAccess, Logger.StaticLogger),
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
