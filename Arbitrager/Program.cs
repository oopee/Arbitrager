using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;
using Kraken;
using Gdax;
using System.Configuration;

namespace Arbitrager
{
    class Program
    {
        static void Main(string[] args)
        {
            //
            Interface.Logger.StaticLogger = new Common.ConsoleLogger();
            //

            var buyer = new KrakenBuyer(new KrakenConfiguration()
            {
                Url = "https://api.kraken.com",
                Version = 0,
                Secret = ConfigurationManager.AppSettings["KrakenSecret"] ?? "",
                Key = ConfigurationManager.AppSettings["KrakenKey"] ?? "",
            }, Logger.StaticLogger);

            var seller = new GdaxSeller(new GdaxConfiguration()
            {
                Key = ConfigurationManager.AppSettings["GdaxKey"] ?? "",
                Signature = ConfigurationManager.AppSettings["GdaxSecret"] ?? "",
                Passphrase = ConfigurationManager.AppSettings["GdaxPassphrase"] ?? "",
            }, Logger.StaticLogger, isSandbox: false);

            // buyer.PlaceBuyOrder(price: 1m, volume: 0.1m).Wait();
            // seller.PlaceSellOrder(price: 999m, volume: 0.01m).Wait();

            Do(buyer, seller).Wait();
            Console.ReadLine();
        }

        static async Task Do(IBuyer buyer, ISeller seller)
        {
            IArbitrager arbitrager = new Common.DefaultArbitrager(buyer, seller, Logger.StaticLogger);
            var result = await arbitrager.GetStatus(false);
            Logger.StaticLogger.Info(result.ToString());
        }
    }   
}
