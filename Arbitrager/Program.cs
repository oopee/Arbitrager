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

            var dataAccess = new DatabaseAccess.DatabaseAccess();
            dataAccess.ResetDatabase().Wait();

            IBuyer buyer;
            ISeller seller;

            var configuration = Utils.AppConfigLoader.Instance.AppSettings("configuration");

            switch (configuration)
            {
                case "simulated":
                    buyer = new SimulatedKrakenBuyer(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger) { BalanceEth = 0m, BalanceEur = 2000m };
                    seller = new SimulatedGdaxSeller(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false) { BalanceEth = 10m, BalanceEur = 0m };
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

            var app = new App(
                new ConsoleArbitrager(buyer, seller, new Common.DefaultProfitCalculator(), dataAccess, Logger.StaticLogger),
                dataAccess,
                Logger.StaticLogger);
            app.Run().Wait();
        }
    }   

    public class App
    {
        IArbitrager m_arbitrager;
        ILogger m_logger;
        IDatabaseAccess m_dataAccess;

        public App(IArbitrager arbitrager, IDatabaseAccess dataAccess, ILogger logger)
        {
            m_arbitrager = arbitrager;
            m_logger = logger;
            m_dataAccess = dataAccess;
        }

        public async Task Run()
        {
            m_logger.Info("Starting, please wait...");
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
            var info = await m_arbitrager.GetInfoForArbitrage(eur);
            if (verb == "info")
            {
                Console.WriteLine(info.ToString());
            }
            else if (verb == "do")
            {
                await m_arbitrager.Arbitrage(ArbitrageContext.Start(info.TargetFiatToSpend));
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
            var status = await m_arbitrager.GetStatus(true);
            ProfitCalculation profitCalculation = null;
            if (cashLimit != null)
            {
                profitCalculation = m_arbitrager.ProfitCalculator.CalculateProfit(status.Buyer, status.Seller, cashLimit.Value);
            }

            Console.WriteLine(status.ToString());
            if (profitCalculation != null)
            {
                Console.WriteLine(profitCalculation.ToString());
            }
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

    public class ConsoleArbitrager : Common.DefaultArbitrager
    {
        public ConsoleArbitrager(IBuyer buyer, ISeller seller, IProfitCalculator profitCalculator, IDatabaseAccess dataAccess, ILogger logger)
            : base(buyer, seller, profitCalculator, dataAccess, logger)
        {
        }

        protected override async Task DoArbitrage_CheckStatus(ArbitrageContext ctx)
        {
            await base.DoArbitrage_CheckStatus(ctx);

            if (ctx.Error != null)
            {
                return;
            }

            Console.WriteLine("ARBITRAGE INFO");
            Console.WriteLine(ctx.Info);
            if (!ConsoleUtils.Confirm())
            {
                ctx.Error = ArbitrageError.ManuallyAborted;
            }
        }
    }

    public static class ConsoleUtils
    {
        public static bool Confirm()
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
    }
}
