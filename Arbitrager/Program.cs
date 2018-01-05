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
            IDatabaseAccess db = new DatabaseAccess.DatabaseAccess();
            db.ResetDatabase().Wait();
            db.TestAsync().Wait();

            //
            Interface.Logger.StaticLogger = new Common.ConsoleLogger();
            //

            var buyer = new KrakenBuyer(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger);
            var seller = new GdaxSeller(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false);

            var app = new App(
                new Common.DefaultArbitrager(buyer, seller, new Common.DefaultProfitCalculator(), Logger.StaticLogger), 
                Logger.StaticLogger);
            app.Run().Wait();
        }
    }   

    public class App
    {
        IArbitrager m_arbitrager;
        ILogger m_logger;

        public App(IArbitrager arbitrager, ILogger logger)
        {
            m_arbitrager = arbitrager;
            m_logger = logger;
        }

        public async Task Run()
        {
            Console.WriteLine("Starting, please wait...");
            await ShowStatus();
            ShowHelp();
            await Input();
        }

        private async Task Input()
        {
            while (true)
            {
                Console.Write("> ");
                string cmd = Console.ReadLine();
                var parts = cmd.ToLower().Replace(',', '.').Split(' ');
                if (parts.Length == 0)
                {
                    continue;
                }
                
                switch (parts[0])
                {
                    case "exit":
                    case "quit":
                        return;
                    case "help":
                    case "?":
                        ShowHelp();
                        break;
                    case "status":
                        decimal? cashLimit = null;
                        if (parts.Count() > 1 && decimal.TryParse(parts[1], out var parsedCashLimit))
                        {
                            cashLimit = parsedCashLimit;
                        }

                        await ShowStatus(cashLimit);

                        break;
                    case "accounts":
                        await ShowAccounts();
                        break;
                    case "buy":
                        break;
                    case "sell":
                        break;
                    case "arbitrage":
                        {
                            string verb = Parse<string>(parts.Skip(1)).Item1;
                            decimal? amount = null;
                            if (verb == "do")
                            {
                                if (parts[2] == "max")
                                {
                                    amount = null;
                                }
                                else
                                {
                                    amount = Parse<decimal>(parts.Skip(2))?.Item1;
                                    if (amount == null)
                                    {
                                        continue;
                                    }
                                }
                            }

                            await DoArbitrage(verb, amount);
                        }
                        break;
                }
            }
        }

        private async Task DoArbitrage(string verb, decimal? eur)
        {
            var info = await GetInfoForArbitrage(eur);
            if (verb == "info")
            {
                Console.WriteLine(info.ToString());
            }
            else if (verb == "do")
            {
                Console.WriteLine(info.ToString());
                if (!info.IsProfitable)
                {
                    Console.WriteLine("ABORTING because IsProfitable=false!");
                    return;
                }

                if (!info.IsBalanceSufficient)
                {
                    Console.WriteLine("ABORTING because provided eur sum {0} is too large for current balances!");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine("EXECUTING ARBITRAGE");
                Console.WriteLine("\tBuying  approx. {0:G8} ETH for {1:G8}€ per unit in total of {2:G8} EUR at {3}", info.MaxApproximateEthsToSell, info.BestBuyPrice, info.MaxApproximateEursToSpend, m_arbitrager.Buyer.Name);
                Console.WriteLine("\tSelling approx. {0:G8} ETH for {1:G8}€ per unit in total of {2:G8} EUR at {3}", info.MaxApproximateEthsToSell, info.BestSellPrice, info.MaxApproximateEursToGain, m_arbitrager.Seller.Name);
                bool ok = Confirm();
                if (!ok)
                {
                    Console.WriteLine("ABORTING");
                    return;
                }
                Console.WriteLine("EXECUTING BUY ORDER");
                Console.WriteLine("\tPlacing BID for {0:G8} EUR x {1:G8} ETH = {2:G8} EUR", info.BestBuyPrice, info.MaxApproximateEthsToSell, info.MaxApproximateEursToSpend);
                var buyOrder = await m_arbitrager.Buyer.PlaceBuyOrder(info.BestBuyPrice, info.MaxApproximateEthsToSell);
                Console.WriteLine("\t\tBID placed! OrderId is {0}", buyOrder.Id);
                Console.WriteLine("\tChecking order...");
                var buyOrderInfo = await m_arbitrager.Buyer.GetOrderInfo(buyOrder.Id);
                Console.WriteLine("\t\tOrder is {0}. Filled volume {1:G8} ETH for avg price {2:G8} EUR per ETH for total of {3:G8} EUR (including fee {4:G8} EUR)", buyOrderInfo.State, buyOrderInfo.FilledVolume, buyOrderInfo.PricePerUnit, buyOrderInfo.FilledVolume * buyOrderInfo.PricePerUnit + buyOrderInfo.Fee, buyOrderInfo.Fee);
                if (buyOrderInfo.State == OrderState.Open)
                {
                    Console.WriteLine("\tCancelling order...");
                    await m_arbitrager.Buyer.CancelOrder(buyOrder.Id);
                    Console.WriteLine("\tCancelled... checking order...");

                    buyOrderInfo = await m_arbitrager.Buyer.GetOrderInfo(buyOrder.Id);
                    Console.WriteLine("\t\tOrder is {0}. Filled volume {1:G8} ETH for avg price {2:G8} EUR per ETH for total of {3:G8} EUR (including fee {4:G8} EUR)", buyOrderInfo.State, buyOrderInfo.FilledVolume, buyOrderInfo.PricePerUnit, buyOrderInfo.FilledVolume * buyOrderInfo.PricePerUnit + buyOrderInfo.Fee, buyOrderInfo.Fee);
                }

                if (buyOrderInfo.FilledVolume == 0m)
                {
                    Console.WriteLine("ABORTING! Order could not be filled even partially.");
                    return;
                }

                decimal ethToSell = buyOrderInfo.FilledVolume;
                Console.WriteLine("\tFinal ETH amount is {0}", ethToSell);

                Console.WriteLine();
                Console.WriteLine("EXECUTING SELL ORDER");
                Console.WriteLine("\tPlacing ASK for {0:G8} EUR x {1:G8} ETH = {2:G8} EUR", info.BestSellPrice, ethToSell, info.BestSellPrice * ethToSell);
                var sellOrder = await m_arbitrager.Seller.PlaceSellOrder(info.BestSellPrice, ethToSell);
                Console.WriteLine("\tASK placed! OrderId is {0}", sellOrder.Id);
                var sellOrderInfo = await m_arbitrager.Seller.GetOrderInfo(sellOrder.Id);
                Console.WriteLine("\t\tOrder is {0}. Filled volume {1:G8} ETH for avg price {2:G8} EUR per ETH for total of {3:G8} EUR (including fee {4:G8} EUR)", sellOrderInfo.State, sellOrderInfo.FilledVolume, sellOrderInfo.PricePerUnit, sellOrderInfo.FilledVolume * sellOrderInfo.PricePerUnit + sellOrderInfo.Fee, sellOrderInfo.Fee);

                if (sellOrderInfo.State == OrderState.Open)
                {
                    Console.WriteLine("\tCancelling order...");
                    await m_arbitrager.Seller.CancelOrder(sellOrder.Id);
                    Console.WriteLine("\tCancelled... checking order...");

                    sellOrderInfo = await m_arbitrager.Seller.GetOrderInfo(sellOrder.Id);
                    Console.WriteLine("\t\tOrder is {0}. Filled volume {1:G8} ETH for avg price {2:G8} EUR per ETH for total of {3:G8} EUR (including fee {4:G8} EUR)", sellOrderInfo.State, sellOrderInfo.FilledVolume, sellOrderInfo.PricePerUnit, sellOrderInfo.FilledVolume * sellOrderInfo.PricePerUnit + sellOrderInfo.Fee, sellOrderInfo.Fee);
                }

                if (ethToSell != sellOrderInfo.FilledVolume)
                {
                    Console.WriteLine("** WARNING **: Could not sell all ETH that was bought. Bought {0} ETH, sold {1} ETH, diff {2} ETH", ethToSell, sellOrderInfo.FilledVolume, ethToSell - sellOrderInfo.FilledVolume);
                }

                Console.WriteLine("FINISHED!");

                await ShowStatus();
            }            
        }

        private bool Confirm()
        {
            while (true)
            {
                Console.Write("Do you want to continue (Y/N)> ");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    return true;
                }
                else if (answer.ToLower() == "n")
                {
                    return false;
                }
            }
        }

        public async Task<ArbitrageInfo> GetInfoForArbitrage(decimal? maxEursToSpendArg)
        {
            var status = await m_arbitrager.GetStatus(true, maxEursToSpendArg);

            ArbitrageInfo info = new ArbitrageInfo();
            info.MaxNegativeSpreadPercentage = status.Difference.MaxNegativeSpreadPercentage;
            info.MaxNegativeSpreadEur = status.Difference.MaxNegativeSpread;
            info.EurBalance = status.Buyer.Balance.Eur;
            info.EthBalance = status.Seller.Balance.Eth;
            info.BestBuyPrice = status.Buyer.Asks.Asks[0].PricePerUnit + 1;
            info.BestSellPrice = status.Seller.Bids.Bids[0].PricePerUnit - 1;

            var maxEursToSpend = maxEursToSpendArg != null ? Math.Min(maxEursToSpendArg.Value, info.EurBalance) : info.EurBalance;

            if (status.Difference.MaxNegativeSpreadPercentage < 0.02m) // 2%
            {
                info.IsProfitable = false;
            }
            else
            {
                info.IsProfitable = true;
                var eursToUse = (maxEursToSpend * 0.99m - 1);// note: we subtract 1 eur and 1% from eur balance to cover fees
                if (eursToUse > 0)
                {
                    decimal maxApproxEthsToBuy = eursToUse / info.BestBuyPrice;                    

                    decimal maxApproxEthsToArbitrage;
                    if (maxApproxEthsToBuy >= status.Seller.Balance.Eth) // check if we have enough ETH to sell
                    {
                        maxApproxEthsToArbitrage = status.Seller.Balance.Eth * 0.99m; // note: subtract 1% to avoid rounding/fee errors
                    }
                    else
                    {
                        maxApproxEthsToArbitrage = maxApproxEthsToBuy;
                    }

                    maxApproxEthsToArbitrage = decimal.Round(maxApproxEthsToArbitrage, 2, MidpointRounding.ToEven);


                    info.MaxApproximateEthsToSell = maxApproxEthsToArbitrage;
                    info.MaxApproximateEursToSpend = maxApproxEthsToArbitrage * info.BestBuyPrice;
                    info.MaxApproximateEursToGain = maxApproxEthsToArbitrage * info.BestSellPrice;

                    info.MaxApproximateEurProfit = (info.MaxApproximateEursToGain * 0.995m) - info.MaxApproximateEursToSpend; // subtract 0.5% to cover fees
                }
            }

            info.BuyerName = m_arbitrager.Buyer.Name;
            info.SellerName = m_arbitrager.Seller.Name;
            info.IsBalanceSufficient = info.EurBalance >= info.MaxApproximateEursToSpend;

            return info;
        }

        public class ArbitrageInfo
        {
            public string BuyerName { get; set; }
            public string SellerName { get; set; }

            public bool IsProfitable { get; set; }
            public bool IsBalanceSufficient { get; set; }
            public decimal MaxNegativeSpreadPercentage { get; set; }
            public decimal MaxNegativeSpreadEur { get; set; }
            public decimal EurBalance { get; set; }
            public decimal EthBalance { get; set; }
            public decimal MaxApproximateEthsToSell { get; set; }
            public decimal MaxApproximateEursToSpend { get; set; }
            public decimal MaxApproximateEursToGain { get; set; }
            public decimal MaxApproximateEurProfit { get; set; }

            public decimal BestBuyPrice { get; set; }
            public decimal BestSellPrice { get; set; }

            public override string ToString()
            {
                StringBuilder b = new StringBuilder();
                b.AppendLine("ARBITRAGE INFO");
                b.AppendLine("\tEUR balance at {0}: {1}", BuyerName, EurBalance);
                b.AppendLine("\tETH balance at {0}: {1}", SellerName, EthBalance);
                b.AppendLine("\tBest buy price        : {0}", BestBuyPrice);
                b.AppendLine("\tBest sell price       : {0}", BestSellPrice);
                b.AppendLine("\tMax negative spread   : {0}", MaxNegativeSpreadEur);
                b.AppendLine("\tMax negative spread % : {0}", MaxNegativeSpreadPercentage * 100);
                b.AppendLine("\tIs profitable         : {0}", IsProfitable ? "Yes" : "No");
                b.AppendLine("\tIs balance sufficient : {0}", IsBalanceSufficient ? "Yes" : "No");
                b.AppendLine("\tEstimated buy         : {0:G8} EUR -> {1:G8} ETH", MaxApproximateEursToSpend, MaxApproximateEthsToSell);
                b.AppendLine("\tEstimated sell        : {0:G8} ETH -> {1:G8} EUR", MaxApproximateEthsToSell, MaxApproximateEursToGain);
                b.AppendLine("\tEstimated profit      : {0} EUR", MaxApproximateEurProfit);

                return b.ToString();
            }
        }

        private Tuple<T1> Parse<T1>(IEnumerable<string> data)
        {
            var d = data.ToList();
            bool ok = false;
            var r = Tuple.Create((T1)Parse(typeof(T1), d[0], ref ok));
            return ok ? r : null;
        }

        private Tuple<T1, T2> Parse<T1, T2>(IEnumerable<string> data)
        {
            var d = data.ToList();
            bool ok = false;
            var r = Tuple.Create(
                (T1)Parse(typeof(T1), d[0], ref ok),
                (T2)Parse(typeof(T2), d[1], ref ok));
            return ok ? r : null;
        }

        private Tuple<T1, T2, T3> Parse<T1, T2, T3>(IEnumerable<string> data)
        {
            var d = data.ToList();
            bool ok = false;
            var r = Tuple.Create(
                (T1)Parse(typeof(T1), d[0], ref ok),
                (T2)Parse(typeof(T2), d[1], ref ok),
                (T3)Parse(typeof(T3), d[1], ref ok));
            return ok ? r : null;
        }

        private object Parse(Type type, string data, ref bool ok)
        {
            if (type == typeof(string))
            {
                ok |= true;
                return data;
            }
            else if (type == typeof(int))
            {
                int result;
                if (int.TryParse(data, out result))
                {
                    ok |= true;
                    return result;
                }
            }
            else if (type == typeof(decimal))
            {
                decimal result;
                if (decimal.TryParse(data, out result))
                {
                    ok |= true;
                    return result;
                }
            }
            
            m_logger.Error("Invalid {0} value '{1}'", type.Name.ToLower(), data);
            return null;
        }

        private async Task ShowStatus(decimal? cashLimit = null)
        {
            var status = await m_arbitrager.GetStatus(true, cashLimit);
            Console.WriteLine(status.ToString());
            Console.WriteLine();
        }

        private async Task ShowAccounts()
        {
            var accounts = await m_arbitrager.GetAccountsInfo();
            Console.WriteLine(accounts.ToString());
            Console.WriteLine();
        }

        private void ShowHelp()
        {
            Console.WriteLine("COMMANDS");
            Console.WriteLine("\tstatus [cash limit]");
            Console.WriteLine("\taccounts");
            // Console.WriteLine("\tbuy market (eur amount)");
            // Console.WriteLine("\tsell market (eth amount)");
            Console.WriteLine("\tarbitrage info");
            Console.WriteLine("\tarbitrage do (eur amount OR \"max\")");
            Console.WriteLine("\texit");
        }
    }
}
