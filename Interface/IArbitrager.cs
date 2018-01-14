using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IArbitrager
    {
        IBuyer Buyer { get; }
        ISeller Seller { get; }
        Task<Status> GetStatus(bool includeBalance);
        Task<AccountsInfo> GetAccountsInfo();
        IProfitCalculator ProfitCalculator { get; }
        Task<ArbitrageContext> Arbitrage(ArbitrageContext ctx);
        Task<ArbitrageInfo> GetInfoForArbitrage(PriceValue maxFiatToSpend, BalanceOption fiatOptions, PriceValue maxEthToSpend, BalanceOption ethOptions);
    }

    public enum BalanceOption
    {
        IgnoreBalance,
        CapToBalance
    }

    public class ArbitrageResult
    {
        public bool Aborted { get; set; }
    }

    public enum ArbitrageState
    {
        NotStarted,
        CheckStatus,
        PlaceBuyOrder,
        GetBuyOrderInfo,
        PlaceSellOrder,
        GetSellOrderInfo,
        CalculateFinalResult,
        Finished
    }

    public enum ArbitrageError
    {
        Unknown,
        ManuallyAborted,
        InvalidBalance,
        ZeroEthBought,
        CouldNotPlaceBuyOrder
    }

    public class ArbitrageContext
    {
        /// <summary>
        /// Current state.
        /// </summary>
        public ArbitrageState State { get; set; }

        public ArbitrageState? BreakOnState { get; set; }

        public bool SpendWholeBalance { get; set; }
        /// <summary>
        /// The amount of fiat that user wants to spend
        /// </summary>
        public PriceValue UserFiatToSpend { get; set; }

        // For PlaceBuyOrder
        public PriceValue? BuyOrder_LimitPriceToUse { get; set; }
        public PriceValue? BuyOrder_EthAmountToBuy { get; set; }

        public ArbitrageError? Error { get; set; }
        public ArbitrageInfo Info { get; set; }

        public OrderId? BuyOrderId { get; set; }
        public PriceValue? BuyEthAmount { get; set; }
        public OrderId? SellOrderId { get; set; }
        public ILogger Logger { get; set; }

        public FullOrder BuyOrder { get; set; }
        public FullOrder SellOrder { get; set; }

        public FinishedResultData FinishedResult { get; set; }

        public bool AbortIfFiatToSpendIsMoreThanBalance { get; set; } = true;

        public static ArbitrageContext Start(PriceValue? fiatToSpend)
        {
            var ctx = new ArbitrageContext()
            {
                SpendWholeBalance = fiatToSpend == null,
                UserFiatToSpend = fiatToSpend ?? PriceValue.FromEUR(0),
                State = ArbitrageState.NotStarted
            };

            return ctx;
        }

        public static ArbitrageContext GetInfo(PriceValue? fiatToSpend)
        {
            var ctx = Start(fiatToSpend);
            ctx.BreakOnState = ArbitrageState.PlaceBuyOrder;
            return ctx;
        }

        public class FinishedResultData
        {
            public PriceValue EthBought { get; set; }
            public PriceValue EthSold { get; set; }
            public PriceValue FiatSpent { get; set; }
            public PriceValue FiatEarned { get; set; }

            public BalanceResult BuyerBalance { get; set; }
            public BalanceResult SellerBalance { get; set; }

            public PriceValue FiatDelta => FiatEarned - FiatSpent;
            public PriceValue EthDelta => EthSold - EthBought;
            public PercentageValue ProfitPercentage => FiatSpent == 0m ? PercentageValue.Zero : PercentageValue.FromRatio((FiatDelta / FiatSpent).Value);

            public override string ToString() => string.Format("EthBought {0} | EthSold {1} | EthDelta {2} | FiatSpent {3} | FiatEarned {4} | FiatDelta {5} | BuyerBalance {6:0.##} EUR, {7:0.####} ETH | SellerBalancer {8:0.##} EUR, {9:0.####} ETH", EthBought, EthSold, EthDelta, FiatSpent, FiatEarned, FiatDelta, BuyerBalance.Eur, BuyerBalance.Eth, SellerBalance.Eur, SellerBalance.Eth);
        }
    }

    public class ArbitrageInfo
    {
        public Status Status { get; set; }
        public ProfitCalculation ProfitCalculation { get; set; }

        public string BuyerName => Status.Buyer.Name;
        public string SellerName => Status.Seller.Name;
        public PercentageValue MaxNegativeSpreadPercentage => Status.Difference.MaxNegativeSpreadPercentage;
        public PriceValue MaxNegativeSpreadEur => Status.Difference.MaxNegativeSpread;
        public PriceValue EurBalance => Status.Buyer.Balance.Eur;
        public PriceValue EthBalance => Status.Seller.Balance.Eth;

        public PriceValue MaxEthAmountToArbitrage => ProfitCalculation.EthsToArbitrage;
        public PriceValue MaxEursToSpend => ProfitCalculation.FiatSpent;
        public PriceValue MaxEursToEarn => ProfitCalculation.FiatEarned;
        public PriceValue MaxEurProfit => ProfitCalculation.Profit;
        public PercentageValue MaxProfitPercentage => ProfitCalculation.ProfitPercentage;
        public PriceValue MaxBuyFee => ProfitCalculation.BuyFee;
        public PriceValue MaxSellFee => ProfitCalculation.SellFee;

        public PriceValue EstimatedAvgBuyUnitPrice => ProfitCalculation.EthBuyCount > 0 ? (ProfitCalculation.FiatSpent / ProfitCalculation.EthBuyCount.Value) : PriceValue.FromEUR(0);
        public PriceValue EstimatedAvgSellUnitPrice => ProfitCalculation.EthSellCount > 0 ? (ProfitCalculation.FiatEarned / ProfitCalculation.EthSellCount.Value) : PriceValue.FromEUR(0);
        public PriceValue EstimatedAvgNegativeSpread => EstimatedAvgSellUnitPrice - EstimatedAvgBuyUnitPrice;
        public PercentageValue EstimatedAvgNegativeSpreadPercentage => EstimatedAvgBuyUnitPrice.Value > 0 ? PercentageValue.FromRatio(EstimatedAvgNegativeSpread.Value / EstimatedAvgBuyUnitPrice.Value) : PercentageValue.Zero;

        public PriceValue BestBuyPrice => PriceValue.FromEUR(Status.Buyer.Asks.Asks.FirstOrDefault()?.PricePerUnit ?? 0m);
        public PriceValue BestSellPrice => PriceValue.FromEUR(Status.Seller.Bids.Bids.FirstOrDefault()?.PricePerUnit ?? 0m);

        public PriceValue BuyLimitPricePerUnit => ProfitCalculation.BuyLimitPricePerUnit;

        public bool IsEurBalanceSufficient => ProfitCalculation.FiatSpent <= Status.Buyer.Balance.Eur;
        public bool IsEthBalanceSufficient => ProfitCalculation.EthSellCount <= Status.Seller.Balance.Eth;
        public bool IsProfitable { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("ARBITRAGE INFO");
            b.AppendLine("\tEUR balance at {0}: {1}", BuyerName, EurBalance);
            b.AppendLine("\tETH balance at {0}: {1}", SellerName, EthBalance);
            b.AppendLine();
            b.AppendLine("\tAvg. neg. spread % (i. fees): {0}", EstimatedAvgNegativeSpreadPercentage);
            b.AppendLine("\tAvg. neg. spread (inc. fees): {0:0.##} EUR", EstimatedAvgNegativeSpread);
            b.AppendLine("\tAvg. buy price (incl. fees) : {0:0.##} EUR", EstimatedAvgBuyUnitPrice);
            b.AppendLine("\tAvg. sell price (incl. fees): {0:0.##} EUR", EstimatedAvgSellUnitPrice);
            b.AppendLine();
            b.AppendLine("\tMax negative spread %       : {0}", MaxNegativeSpreadPercentage);
            b.AppendLine("\tMax negative spread         : {0:0.##} EUR", MaxNegativeSpreadEur);
            b.AppendLine("\tBest buy price              : {0:0.##} EUR", BestBuyPrice);
            b.AppendLine("\tBest sell price             : {0:0.##} EUR", BestSellPrice);
            b.AppendLine();
            b.AppendLine("\tETHs to arbitrage           : {0:0.##} ETH", MaxEthAmountToArbitrage);
            b.AppendLine("\tBuy limit price (per unit)  : {0:0.##} ETH", BuyLimitPricePerUnit);
            b.AppendLine("\tEstimated buy fee           : {0:0.##} EUR", MaxBuyFee);
            b.AppendLine("\tEstimated sell fee          : {0:0.##} EUR", MaxSellFee);
            b.AppendLine("\tEstimated buy (incl. fees)  : {0:0.##} EUR -> {1:0.##} ETH", MaxEursToSpend, MaxEthAmountToArbitrage);
            b.AppendLine("\tEstimated sell (incl. fees) : {0:0.##} ETH -> {1:0.##} EUR", MaxEthAmountToArbitrage, MaxEursToEarn);
            b.AppendLine("\tEstimated profit            : {0:0.##} EUR", MaxEurProfit);
            b.AppendLine("\tEstimated profit %          : {0}", MaxProfitPercentage);
            b.AppendLine();
            b.AppendLine("\tIs profitable               : {0}", IsProfitable ? "Yes" : "No");
            b.AppendLine("\tIs EUR balance sufficient   : {0}", IsEurBalanceSufficient ? "Yes" : "No");
            b.AppendLine("\tIs ETH balance sufficient   : {0}", IsEthBalanceSufficient ? "Yes" : "No");

            return b.ToString();
        }
    }

    public class AccountsInfo
    {
        public List<AccountInfo> Accounts { get; set; } = new List<AccountInfo>();

        public override string ToString()
        {
            return string.Join("\n\n", Accounts);
        }
    }

    public class AccountInfo
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendLine("ACCOUNT");
            b.AppendLine("\t{0}", Name);
            b.AppendLine("\tBalance:");
            b.AppendLine("\t\tEUR: {0}", Balance.Eur);
            b.AppendLine("\t\tETH: {0}", Balance.Eth);
            b.AppendLine();
            b.AppendLine("\tPayment methods:");
            if (PaymentMethods.Count > 0)
            {
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", PaymentMethods.Select(x => x.Name)));
            }
            else
            {
                b.AppendLine("\t\tNone");
            }

            return b.ToString();
        }
    }

    public class Status
    {
        public Status(BuyerStatus buyer, SellerStatus seller)
        {
            Buyer = buyer;
            Seller = seller;

            if (Buyer != null && Seller != null)
            {
                Difference = new DifferenceStatus()
                {
                    MaxNegativeSpread = PriceValue.FromEUR(0),
                    MaxNegativeSpreadPercentage = PercentageValue.Zero
                };

                if (Buyer.Asks.Asks.Count > 0 && Seller.Bids.Bids.Count > 0)
                {
                    var negativeSpread = Buyer.Asks.Asks[0].PricePerUnit - Seller.Bids.Bids[0].PricePerUnit;
                    if (negativeSpread < 0) 
                    {
                        Difference.MaxNegativeSpread = PriceValue.FromEUR(Math.Abs(negativeSpread));
                        Difference.MaxNegativeSpreadPercentage = PercentageValue.FromRatio(Difference.MaxNegativeSpread.Value / Buyer.Asks.Asks[0].PricePerUnit);
                    }
                }
            }
        }

        public BuyerStatus Buyer { get; set; }
        public SellerStatus Seller { get; set; }
        public DifferenceStatus Difference { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendLine("BUY");
            if (Buyer == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t{0}", Buyer.Name);
                if (Buyer.Balance != null)
                {
                    b.AppendLine("\tBalance:");
                    b.AppendLine("\t\tEUR: {0}", Buyer.Balance.Eur);
                    b.AppendLine("\t\tETH: {0}", Buyer.Balance.Eth);
                }
                b.AppendLine("\tAsks (best)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", Buyer.Asks.Asks.Take(1)));
            }

            b.AppendLine();
            b.AppendLine("SELL");
            if (Seller == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t{0}", Seller.Name);
                if (Seller.Balance != null)
                {
                    b.AppendLine("\tBalance:");
                    b.AppendLine("\t\tEUR: {0}", Seller.Balance.Eur);
                    b.AppendLine("\t\tETH: {0}", Seller.Balance.Eth);
                }
                /*b.AppendLine("\tChunk");
                b.AppendLine("\t\tMax euros to use: {0}", ChunkEur);
                b.AppendLine("\t\tActual euros to use: {0}", eurToBuy);*/
                b.AppendLine("\tBids (best)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", Seller.Bids.Bids.Take(1)));
            }

            b.AppendLine();
            b.AppendLine("DIFFERENCE");
            if (Difference == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t\tMax. negative spread: {0}€", Difference.MaxNegativeSpread);
                b.AppendLine("\t\tMax. negative spread: {0} (of lowest ask)", (Difference.MaxNegativeSpreadPercentage * 100));
            }

            return b.ToString();
        }

        private void AddProfitCalculation(StringBuilder b, ProfitCalculation profitCalculation)
        {
            var calc = profitCalculation;

            if (calc.FiatSpent <= 0m)
            {
                return;
            }

            b.AppendLine();

            string moneySpentSummary = string.Format("{0:0.00}e", calc.FiatSpent);
            if (!calc.AllFiatSpent)
            {
                moneySpentSummary += " (capped, no more bids at other exchange)";
            }

            b.AppendLine("\t\tCash limit:\t\t\t{0}", moneySpentSummary);
            b.AppendLine("\t\tETH available to buy:\t\t{0:0.0000}", calc.EthBuyCount);
            b.AppendLine("\t\tETH available to sell:\t\t{0:0.0000}", calc.EthSellCount);
            b.AppendLine("\t\tETH value at other exchange:\t{0:0.00}e", calc.FiatEarned);
            b.AppendLine("\t\tProfit:\t\t\t\t{0:0.00}e ({1})", calc.Profit, PercentageValue.FromRatio((calc.Profit / calc.FiatSpent).Value));
            b.AppendLine("\t\tProfit after tax:\t\t{0:0.00}e ({1:0.00}%)", calc.ProfitAfterTax, PercentageValue.FromRatio((calc.ProfitAfterTax / calc.FiatSpent).Value));
        }
    }

    public class BuyerStatus
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public IAskOrderBook Asks { get; set; }

        public PercentageValue TakerFee { get; set; }
        public PercentageValue MakerFee { get; set; }
    }

    public class SellerStatus
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public IBidOrderBook Bids { get; set; }

        public PercentageValue TakerFee { get; set; }
        public PercentageValue MakerFee { get; set; }
    }

    public class FeeInfo
    {
        public decimal TakerFee { get; set; }
        public decimal MakerFee { get; set; }
    }

    public class DifferenceStatus
    {
        /// <summary>
        /// Absolute value of negative spead (negative spread -> ask is lower than bid). If spread is positive, then this value is zero.
        /// </summary>
        public PriceValue MaxNegativeSpread { get; set; }

        /// <summary>
        /// Ratio MaxNegativeSpread / LowestAskPrice.
        /// </summary>
        public PercentageValue MaxNegativeSpreadPercentage { get; set; }
    }

    public class PaymentMethod
    {
        public PaymentMethodId Id { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public object RawResult { get; set; }
    }
}
