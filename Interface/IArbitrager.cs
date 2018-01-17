using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IArbitrager
    {
        AssetPair AssetPairToUse { get; }
        IEnumerable<IExchange> Exchanges { get; }
        Task<Status> GetStatus(bool includeBalance);
        Task<AccountsInfo> GetAccountsInfo();
        IProfitCalculator ProfitCalculator { get; }
        Task<ArbitrageContext> Arbitrage(ArbitrageContext ctx);
        Task<ArbitrageInfo> GetInfoForArbitrage(PriceValue maxQuoteCurrencyToSpend, BalanceOption quoteCurrencyOptions, PriceValue maxBaseCurrencyToSpend, BalanceOption baseCurrencyOptions);

        event EventHandler<ArbitrageContext> StateChanged;
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
        ZeroBaseCurrencyBought,
        CouldNotPlaceBuyOrder,
        CouldNotPlaceSellOrder,
    }

    public class ArbitrageContext
    {
        public AssetPair AssetPair { get; set; }
        public IExchange Buyer { get; set; }
        public IExchange Seller { get; set; }

        /// <summary>
        /// Current state.
        /// </summary>
        public ArbitrageState State { get; set; }

        public ArbitrageState? BreakOnState { get; set; }

        public bool SpendWholeBalance { get; set; }
        /// <summary>
        /// The amount of fiat that user wants to spend
        /// </summary>
        public PriceValue UserDefinedQuoteCurrencyToSpend { get; set; }

        // For PlaceBuyOrder
        public PriceValue? BuyOrder_QuoteCurrencyLimitPriceToUse { get; set; }
        public PriceValue? BuyOrder_BaseCurrencyAmountToBuy { get; set; }

        public ArbitrageError? Error { get; set; }
        public ArbitrageInfo Info { get; set; }

        public OrderId? BuyOrderId { get; set; }
        public PriceValue? BuyBaseCurrencyAmount { get; set; }
        public OrderId? SellOrderId { get; set; }
        public ILogger Logger { get; set; }

        public FullOrder BuyOrder { get; set; }
        public FullOrder SellOrder { get; set; }

        public FinishedResultData FinishedResult { get; set; }

        public bool AbortIfQuoteCurrencyToSpendIsMoreThanBalance { get; set; } = true;

        public static ArbitrageContext Start(AssetPair assetPair, decimal? quoteCurrencyToSpend)
        {
            var ctx = new ArbitrageContext()
            {
                AssetPair = assetPair,
                SpendWholeBalance = quoteCurrencyToSpend == null,
                UserDefinedQuoteCurrencyToSpend = new PriceValue(quoteCurrencyToSpend ?? 0m, assetPair.Quote),
                State = ArbitrageState.NotStarted
            };

            return ctx;
        }

        public static ArbitrageContext GetInfo(AssetPair assetPair, decimal? sourceCurrencyToSpend)
        {
            var ctx = Start(assetPair, sourceCurrencyToSpend);
            ctx.BreakOnState = ArbitrageState.PlaceBuyOrder;
            return ctx;
        }

        public class FinishedResultData
        {
            public PriceValue BaseCurrencyBought { get; set; }
            public PriceValue BaseCurrencySold { get; set; }
            public PriceValue QuoteCurrencySpent { get; set; }
            public PriceValue QuoteCurrencyEarned { get; set; }

            public BalanceResult BuyerBalance { get; set; }
            public BalanceResult SellerBalance { get; set; }

            public PriceValue QuoteCurrencyDelta => QuoteCurrencyEarned - QuoteCurrencySpent;
            public PriceValue BaseCurrencyDelta => BaseCurrencySold - BaseCurrencyBought;
            public PercentageValue ProfitPercentage => QuoteCurrencySpent == 0m ? PercentageValue.Zero : PercentageValue.FromRatio((QuoteCurrencyDelta / QuoteCurrencySpent).Value);

            public override string ToString() => string.Format("BaseBought {0} | BaseSold {1} | BaseDelta {2} | QuoteSpent {3} | QuoteEarned {4} | QuoteDelta {5} | BuyerBalance {6}, {7} | SellerBalancer {8}, {9}",
                BaseCurrencyBought, 
                BaseCurrencySold, 
                BaseCurrencyDelta, 
                QuoteCurrencySpent, 
                QuoteCurrencyEarned, 
                QuoteCurrencyDelta, 
                BuyerBalance.QuoteCurrency.ToStringWithAsset(), 
                BuyerBalance.BaseCurrency.ToStringWithAsset(), 
                SellerBalance.QuoteCurrency.ToStringWithAsset(), 
                SellerBalance.BaseCurrency.ToStringWithAsset());
        }
    }

    public class Status
    {
        public List<ExchangeStatus> Exchanges { get; set; } = new List<ExchangeStatus>();

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            foreach (var e in Exchanges)
            {
                b.AppendLine(e.ToString());
                b.AppendLine();
            }
            return b.ToString();
        }
    }

    public class ArbitrageInfo
    {
        public AssetPair AssetPair { get; set; }

        public ExchangeStatus Buyer { get; set; }
        public ExchangeStatus Seller { get; set; }

        public ProfitCalculation ProfitCalculation { get; set; }

        public string BuyerName => Buyer.Name;
        public string SellerName => Seller.Name;
        public PercentageValue MaxNegativeSpreadPercentage => BestBuyPrice.Value == 0m ? PercentageValue.Zero : PercentageValue.FromRatio(MaxNegativeSpread.Value / BestBuyPrice.Value);
        public PriceValue MaxNegativeSpread => BestSellPrice - BestBuyPrice;
        public PriceValue BaseCurrencyBalance => Buyer.Balance.QuoteCurrency;
        public PriceValue QuoteCurrencyBalance => Seller.Balance.BaseCurrency;

        public PriceValue MaxBaseCurrencyAmountToArbitrage => ProfitCalculation.BaseCurrencyAmountToArbitrage;
        public PriceValue MaxQuoteCurrencyAmountToSpend => ProfitCalculation.QuoteCurrencySpent;
        public PriceValue MaxQuoteCurrencyToEarn => ProfitCalculation.QuoteCurrencyEarned;
        public PriceValue MaxQuoteCurrencyProfit => ProfitCalculation.Profit;
        public PercentageValue MaxProfitPercentage => ProfitCalculation.ProfitPercentage;
        public PriceValue MaxBuyFee => ProfitCalculation.BuyFee;
        public PriceValue MaxSellFee => ProfitCalculation.SellFee;

        public PriceValue EstimatedAvgBuyUnitPrice => ProfitCalculation.BaseCurrencyBuyCount > 0 ? (ProfitCalculation.QuoteCurrencySpent / ProfitCalculation.BaseCurrencyBuyCount.Value) : new PriceValue(0m, AssetPair.Quote);
        public PriceValue EstimatedAvgSellUnitPrice => ProfitCalculation.BaseCurrencySellCount > 0 ? (ProfitCalculation.QuoteCurrencyEarned / ProfitCalculation.BaseCurrencySellCount.Value) : new PriceValue(0m, AssetPair.Quote);
        public PriceValue EstimatedAvgNegativeSpread => EstimatedAvgSellUnitPrice - EstimatedAvgBuyUnitPrice;
        public PercentageValue EstimatedAvgNegativeSpreadPercentage => EstimatedAvgBuyUnitPrice.Value > 0 ? PercentageValue.FromRatio(EstimatedAvgNegativeSpread.Value / EstimatedAvgBuyUnitPrice.Value) : PercentageValue.Zero;

        public PriceValue BestBuyPrice => Buyer.OrderBook.BestBuy;
        public PriceValue BestSellPrice => Seller.OrderBook.BestSell;

        public PriceValue BuyLimitPricePerUnit => ProfitCalculation.BuyLimitPricePerUnit;

        public bool IsBaseCurrencyBalanceSufficient => ProfitCalculation.QuoteCurrencySpent <= Buyer.Balance.QuoteCurrency;
        public bool IsQuoteCurrencyBalanceSufficient => ProfitCalculation.BaseCurrencySellCount <= Seller.Balance.BaseCurrency;
        public bool IsProfitable { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("ARBITRAGE INFO");
            b.AppendLine("\t{0} -> {1}", BuyerName, SellerName);
            b.AppendLine("\t{0,-5} balance at {1}: {2}", AssetPair.Base, BuyerName, BaseCurrencyBalance);
            b.AppendLine("\t{0,-5} balance at {1}: {2}", AssetPair.Quote, SellerName, QuoteCurrencyBalance);
            b.AppendLine();
            b.AppendLine("\tAvg. neg. spread % (i. fees): {0}", EstimatedAvgNegativeSpreadPercentage);
            b.AppendLine("\tAvg. neg. spread (inc. fees): {0}", EstimatedAvgNegativeSpread.ToStringWithAsset());
            b.AppendLine("\tAvg. buy price (incl. fees) : {0}", EstimatedAvgBuyUnitPrice.ToStringWithAsset());
            b.AppendLine("\tAvg. sell price (incl. fees): {0}", EstimatedAvgSellUnitPrice.ToStringWithAsset());
            b.AppendLine();
            b.AppendLine("\tMax negative spread %       : {0}", MaxNegativeSpreadPercentage);
            b.AppendLine("\tMax negative spread         : {0}", MaxNegativeSpread.ToStringWithAsset());
            b.AppendLine("\tBest buy price              : {0}", BestBuyPrice.ToStringWithAsset());
            b.AppendLine("\tBest sell price             : {0}", BestSellPrice.ToStringWithAsset());
            b.AppendLine();
            b.AppendLine("\t{0,-5} to arbitrage          : {1}", AssetPair.Base, MaxBaseCurrencyAmountToArbitrage.ToStringWithAsset());
            b.AppendLine("\tBuy limit price (per unit)  : {0}", BuyLimitPricePerUnit.ToStringWithAsset());
            b.AppendLine("\tEstimated buy fee           : {0}", MaxBuyFee.ToStringWithAsset());
            b.AppendLine("\tEstimated sell fee          : {0}", MaxSellFee.ToStringWithAsset());
            b.AppendLine("\tEstimated buy (incl. fees)  : {0} -> {1}", MaxQuoteCurrencyAmountToSpend.ToStringWithAsset(), MaxBaseCurrencyAmountToArbitrage.ToStringWithAsset());
            b.AppendLine("\tEstimated sell (incl. fees) : {0} -> {1}", MaxBaseCurrencyAmountToArbitrage.ToStringWithAsset(), MaxQuoteCurrencyToEarn.ToStringWithAsset());
            b.AppendLine("\tEstimated profit            : {0}", MaxQuoteCurrencyProfit.ToStringWithAsset());
            b.AppendLine("\tEstimated profit %          : {0}", MaxProfitPercentage);
            b.AppendLine();
            b.AppendLine("\tIs profitable               : {0}", IsProfitable ? "Yes" : "No");
            b.AppendLine("\tIs {1,-5} balance sufficient : {0}", IsBaseCurrencyBalanceSufficient ? "Yes" : "No", AssetPair.Base);
            b.AppendLine("\tIs {1,-5} balance sufficient : {0}", IsQuoteCurrencyBalanceSufficient ? "Yes" : "No", AssetPair.Quote);

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
            b.AppendLine("\t\t{0}\t: {1}", Balance.AssetPair.Quote, Balance.QuoteCurrency);
            b.AppendLine("\t\t{0}\t: {1}", Balance.AssetPair.Base, Balance.BaseCurrency);
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
    
    public class ExchangeStatus
    {
        public string Name => Exchange.Name;
        public IExchange Exchange { get; set; }
        public BalanceResult Balance { get; set; }
        public IOrderBook OrderBook { get; set; }

        public PercentageValue TakerFee { get; set; }
        public PercentageValue MakerFee { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("{0}", Name);
            if (Balance != null)
            {
                b.AppendLine("\tBalance:");
                b.AppendLine("\t\t{0}\t: {1}", Balance.AssetPair.Quote, Balance.QuoteCurrency);
                b.AppendLine("\t\t{0}\t: {1}", Balance.AssetPair.Base, Balance.BaseCurrency);
            }
            b.AppendLine("\tBest ask: {0}", OrderBook.Asks.FirstOrDefault());
            b.AppendLine("\tBest bid: {0}", OrderBook.Bids.FirstOrDefault());
            return b.ToString();
        }
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
