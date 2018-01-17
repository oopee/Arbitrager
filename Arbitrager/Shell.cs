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
    public class Shell
    {
        IArbitrager m_arbitrager;
        ILogger m_logger;
        IDatabaseAccess m_dataAccess;
        AssetPair m_assetPair;

        public Shell(AssetPair assetPair, IArbitrager arbitrager, IDatabaseAccess dataAccess, ILogger logger)
        {
            m_arbitrager = arbitrager;
            m_logger = logger;
            m_dataAccess = dataAccess;
            m_assetPair = assetPair;
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

                        await ShowStatus(cashLimit == null ? null : (PriceValue?)new PriceValue(cashLimit.Value, m_assetPair.Quote));

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
                            if (verb == "do" || (verb == "info" && parts.Length >= 3))
                            {
                                if (parts.Length < 3)
                                {
                                    Console.WriteLine("Invalid parameters.");
                                    continue;
                                }
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

        private async Task DoArbitrage(string verb, decimal? quoteCurrency)
        {
            if (verb == "info")
            {
                BalanceOption baseCurrencyOption = quoteCurrency == null ? BalanceOption.CapToBalance : BalanceOption.IgnoreBalance;
                var info = await m_arbitrager.GetInfoForArbitrage(new PriceValue(quoteCurrency ?? decimal.MaxValue, m_assetPair.Quote), baseCurrencyOption, new PriceValue(decimal.MaxValue, m_assetPair.Base), BalanceOption.IgnoreBalance);
                Console.WriteLine(info.ToString());
            }
            else if (verb == "do")
            {
                await m_arbitrager.Arbitrage(ArbitrageContext.Start(m_assetPair, quoteCurrency));
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

        private async Task ShowStatus(PriceValue? cashLimit = null)
        {
            var status = await m_arbitrager.GetStatus(true);
            ProfitCalculation profitCalculation = null;
            if (cashLimit.HasValue)
            {
                profitCalculation = m_arbitrager.ProfitCalculator.CalculateProfit(status.Exchanges[0], status.Exchanges[1], cashLimit.Value);
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
            Console.WriteLine("\tstatus [quoteCurrency limit]");
            Console.WriteLine("\taccounts");
            Console.WriteLine("\tarbitrage info [quoteCurrency amount]");
            Console.WriteLine("\tarbitrage do (quoteCurrency amount OR \"max\")");
            Console.WriteLine("\texit");
        }
    }    
}
