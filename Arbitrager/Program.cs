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
                Secret = ConfigurationManager.AppSettings["KrakenSecret"],
                Key = ConfigurationManager.AppSettings["KrakenKey"],
            });

            var seller = new GdaxSeller(new GdaxConfiguration()
            {
                Key = ConfigurationManager.AppSettings["GdaxKey"],
                Signature = ConfigurationManager.AppSettings["GdaxSecret"],
                Passphrase = ConfigurationManager.AppSettings["GdaxPassphrase"],
            }, isSandbox: false);

            Do(buyer, seller).Wait();
            Console.ReadLine();
        }

        static async Task Do(IBuyer buyer, ISeller seller)
        {
            IArbitrager arbitrager = new DefaultArbitrager(buyer, seller, Logger.StaticLogger);
            var result = await arbitrager.GetStatus();
            Logger.StaticLogger.Info(result);
        }
    }

    public class DefaultArbitrager : IArbitrager
    {
        IBuyer m_buyer;
        ISeller m_seller;
        ILogger m_logger;

        public decimal ChunkEur { get; set; } = 2000m;

        public DefaultArbitrager(IBuyer buyer, ISeller seller, ILogger logger)
        {
            m_buyer = buyer;
            m_seller = seller;
            m_logger = logger;
        }

        public async Task<string> GetStatus()
        {
            BalanceResult buyerBalance = null;
            IAskOrderBook askOrderBook = null;

            BalanceResult sellerBalance = null;
            IBidOrderBook bidOrderBook = null;

            Func<Task> buyerTaskFunc = async () =>
            {
                buyerBalance = await m_buyer.GetCurrentBalance();
                askOrderBook = await m_buyer.GetAsks();
            };

            Func<Task> sellerTaskFunc = async () =>
            {
                sellerBalance = await m_seller.GetCurrentBalance();
                bidOrderBook = await m_seller.GetBids();
            };

            var buyerTask = buyerTaskFunc();
            var sellerTask = sellerTaskFunc();

            await buyerTask;
            await sellerTask;

            StringBuilder b = new StringBuilder();

            if (buyerBalance != null)
            {
                var eurToBuy = Math.Min(ChunkEur, buyerBalance.Eur);
                b.AppendLine();
                b.AppendLine("BUY");
                b.AppendLine("\tBalance:");
                b.AppendLine("\t\tEUR: {0}", buyerBalance.Eur);
                b.AppendLine("\t\tETH: {0}", buyerBalance.Eth);
                b.AppendLine("\tChunk");
                b.AppendLine("\t\tMax euros to use: {0}", ChunkEur);
                b.AppendLine("\t\tActual euros to use: {0}", eurToBuy);
                b.AppendLine("\tAsks (max 5)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", askOrderBook.Asks.Take(5)));
            }

            if (sellerBalance != null)
            {
                b.AppendLine();
                b.AppendLine("SELL");
                b.AppendLine("\tBalance:");
                b.AppendLine("\t\tEUR: {0}", sellerBalance.Eur);
                b.AppendLine("\t\tETH: {0}", sellerBalance.Eth);
                /*b.AppendLine("\tChunk");
                b.AppendLine("\t\tMax euros to use: {0}", ChunkEur);
                b.AppendLine("\t\tActual euros to use: {0}", eurToBuy);*/
                b.AppendLine("\tBids (max 5)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", bidOrderBook.Bids.Take(5)));
            }

            return b.ToString();
        }
    }
}
