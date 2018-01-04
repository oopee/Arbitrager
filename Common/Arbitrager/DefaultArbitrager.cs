using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;

namespace Common
{
    public class DefaultArbitrager : IArbitrager
    {
        IBuyer m_buyer;
        ISeller m_seller;
        IProfitCalculator m_profitCalculator;
        ILogger m_logger;
        IDatabaseAccess m_dataAccess;

        public decimal ChunkEur { get; set; } = 2000m;

        public IBuyer Buyer => m_buyer;
        public ISeller Seller => m_seller;
        public IProfitCalculator ProfitCalculator => m_profitCalculator;

        public DefaultArbitrager(IBuyer buyer, ISeller seller, IProfitCalculator profitCalculator, ILogger logger, IDatabaseAccess dataAccesss)
        {
            m_buyer = buyer;
            m_seller = seller;
            m_profitCalculator = profitCalculator;
            m_logger = logger;
            m_dataAccess = dataAccesss;
        }

        public async Task<AccountsInfo> GetAccountsInfo()
        {
            var buyer = await GetAccountInfo(m_buyer);
            var seller = await GetAccountInfo(m_seller);

            return new AccountsInfo()
            {
                Accounts = new List<AccountInfo>() { buyer, seller }
            };
        }

        private async Task<AccountInfo> GetAccountInfo(IExchange exchange)
        {
            var balance = await exchange.GetCurrentBalance();
            var methods = await exchange.GetPaymentMethods();

            return new AccountInfo()
            {
                Name = exchange.Name,
                Balance = balance,
                PaymentMethods = methods.Methods
            };
        }

        public async Task<Status> GetStatus(bool includeBalance)
        {
            BalanceResult buyerBalance = null;
            IAskOrderBook askOrderBook = null;

            BalanceResult sellerBalance = null;
            IBidOrderBook bidOrderBook = null;

            Func<Task> buyerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    buyerBalance = await m_buyer.GetCurrentBalance();
                }

                askOrderBook = await m_buyer.GetAsks();
            };

            Func<Task> sellerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    sellerBalance = await m_seller.GetCurrentBalance();
                }

                bidOrderBook = await m_seller.GetBids();
            };

            var buyerTask = buyerTaskFunc();
            var sellerTask = sellerTaskFunc();

            await buyerTask;
            await sellerTask;

            BuyerStatus buyerStatus = null;
            if (askOrderBook != null)
            {
                buyerStatus = new BuyerStatus()
                {
                    Name = m_buyer.Name,
                    Asks = askOrderBook,
                    Balance = buyerBalance,
                    MakerFee = m_buyer.MakerFeePercentage,
                    TakerFee = m_buyer.TakerFeePercentage
                };
            }

            SellerStatus sellerStatus = null;
            if (bidOrderBook != null)
            {
                sellerStatus = new SellerStatus()
                {
                    Name = m_seller.Name,
                    Bids = bidOrderBook,
                    Balance = sellerBalance,
                    MakerFee = m_seller.MakerFeePercentage,
                    TakerFee = m_seller.TakerFeePercentage
                };
            }

            return new Status(buyerStatus, sellerStatus);
        }

        public async Task Arbitrage(decimal eur)
        {
            var status = await GetStatus(true);

            if (eur > status.Buyer.Balance.Eur)
            {
                // Requested arbitrage EUR amount is more than we have at exchange B -> abort
                return;
            }

            var ethAvailableToSell = status.Seller.Balance.Eth; // max eth to buy from exchange B is the amount of available ETH at exchange S
            var calc = m_profitCalculator.CalculateProfit(status.Buyer, status.Seller, eur, ethAvailableToSell);

            // GetInfoForArbitrage(eur);
        }

        public async Task<ArbitrageInfo> GetInfoForArbitrage(decimal? maxEursToSpendArg)
        {
            var status = await GetStatus(true);
            var calc = m_profitCalculator.CalculateProfit(status.Buyer, status.Seller, maxEursToSpendArg ?? status.Buyer.Balance.Eur);

            ArbitrageInfo info = new ArbitrageInfo();
            info.MaxNegativeSpreadPercentage = status.Difference.MaxNegativeSpreadPercentage;
            info.MaxNegativeSpreadEur = status.Difference.MaxNegativeSpread;
            info.EurBalance = status.Buyer.Balance.Eur;
            info.EthBalance = status.Seller.Balance.Eth;
            info.BestBuyPrice = status.Buyer.Asks.Asks[0].PricePerUnit + 1;
            info.BestSellPrice = status.Seller.Bids.Bids[0].PricePerUnit - 1;

            var maxEursToSpend = maxEursToSpendArg != null ? Math.Min(maxEursToSpendArg.Value, info.EurBalance) : info.EurBalance;

            if (calc.ProfitPercentage < 0.02m) // 2%
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

            info.BuyerName = Buyer.Name;
            info.SellerName = Seller.Name;
            info.IsBalanceSufficient = info.EurBalance >= info.MaxApproximateEursToSpend;

            return info;
        }
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
}
