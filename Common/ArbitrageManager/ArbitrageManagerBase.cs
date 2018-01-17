using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Interface;

namespace Common.ArbitrageManager
{
    public abstract class ArbitrageManagerBase<TContext> : IArbitrageManager
        where TContext : ArbitrageManagerContext
    {
        bool m_run;
        IArbitrager m_arbitrager;
        ILogger m_logger;
        IClock m_clock;

        bool m_isPaused;
        public bool IsPaused
        {
            get => m_isPaused;
            set
            {
                lock (this)
                {
                    if (m_isPaused != value)
                    {
                        m_isPaused = value;

                        if (m_isPaused)
                        {
                            m_logger.Info("**** PAUSED");
                        }
                        else
                        {
                            m_logger.Info("**** RESUMED");
                        }
                    }
                }
            }
        }

        protected Func<TContext> ContextFactory { get; set; }

        public AssetPair AssetPairToUse => m_arbitrager.AssetPairToUse;

        public ArbitrageManagerBase(IArbitrager arbitrager, ILogger logger, IClock clock = null, Func<TContext> contextFactory = null)
        {
            m_arbitrager = arbitrager;
            m_logger = logger.WithName(GetType().Name);
            m_clock = clock;
            ContextFactory = contextFactory;
        }

        public Task Run()
        {
            if (m_run)
            {
                throw new InvalidOperationException("Already running!");
            }

            m_run = true;
            return Task.Run(DoHeartbeat);
        }

        private async Task DoHeartbeat()
        {
            m_logger.Info("Heartbeat started!");

            var ctx = ContextFactory();
            ctx.Clock = m_clock;
            ctx.Logger = m_logger;

            while (m_run)
            {
                if (!IsPaused)
                {
                    bool resetTime;
                    try
                    {
                        bool ok = await Tick(ctx);
                        resetTime = ok;
                    }
                    catch (Exception e)
                    {
                        m_logger.Error("Exception in Tick(): {0}", e);
                        resetTime = true;
                    }

                    if (resetTime)
                    {
                        ctx.LastAction = ctx.Clock.UtcNow;
                    }
                }

                await Task.Delay(100);
            }
        }

        protected virtual async Task<bool> Tick(TContext ctx)
        {
            var timeSinceLastAction = ctx.GetTimeSinceLastAction();
            if (timeSinceLastAction > TimeSpan.FromSeconds(20))
            {
                m_logger.Info("Tick: Time since last action is {0}. Checking arbitrage status...", timeSinceLastAction);
                await CheckStatusAndDoArbitrage(ctx);
                return true;
            }

            return false;
        }

        protected virtual async Task CheckStatusAndDoArbitrage(TContext ctx)
        {
            // Start arbitrating by asking info (Arbitrage() is a state machine and we ask it to break before placing any orders)
            decimal quoteCurrencyChunk = 100m; // TODO
            var arbitrageCtx = ArbitrageContext.GetInfo(AssetPairToUse, quoteCurrencyChunk);
            arbitrageCtx.AbortIfQuoteCurrencyToSpendIsMoreThanBalance = false;
            await m_arbitrager.Arbitrage(arbitrageCtx);

            string msg = string.Format("\tArbitrageInfo: Profit {0}, {1} | (Bid) {2} - (Ask) {3} = (Spread) {4} | (Orders) {5} -> {6} -> {7} | (Balance) {8}, {9} | (Chunk) {10}",
                arbitrageCtx.Info.MaxProfitPercentage,
                arbitrageCtx.Info.MaxQuoteCurrencyProfit.ToStringWithAsset(),
                arbitrageCtx.Info.BestSellPrice.ToStringWithAsset(),
                arbitrageCtx.Info.BestBuyPrice.ToStringWithAsset(),
                arbitrageCtx.Info.MaxNegativeSpread.ToStringWithAsset(),
                arbitrageCtx.Info.MaxQuoteCurrencyAmountToSpend.ToStringWithAsset(),
                arbitrageCtx.Info.MaxBaseCurrencyAmountToArbitrage.ToStringWithAsset(),
                arbitrageCtx.Info.MaxQuoteCurrencyToEarn.ToStringWithAsset(),
                arbitrageCtx.Info.BaseCurrencyBalance.ToStringWithAsset(),
                arbitrageCtx.Info.QuoteCurrencyBalance.ToStringWithAsset(),
                new PriceValue(quoteCurrencyChunk, AssetPairToUse.Quote).ToStringWithAsset());

            m_logger.Info(msg);

            if (arbitrageCtx.Error != null)
            {
                m_logger.Info("\tArbitrage() returned error {0}. Aborting...", arbitrageCtx.Error);
                return;
            }

            // Determine if we should continue or not
            var result = await ShouldDoArbitrage(ctx, arbitrageCtx.Info);
            m_logger.Info("\tshould continue? {0} ({1})", result.DoArbitrage ? "YES" : "NO", result.Reason);

            if (result.DoArbitrage)
            {
                // YES! GO ON!
                if (result.BuyBaseCurrencyVolume != null)
                {
                    arbitrageCtx.BuyOrder_BaseCurrencyAmountToBuy = result.BuyBaseCurrencyVolume;
                    m_logger.Info("\toverride BaseCurrencyAmountToBuy = {0}", arbitrageCtx.BuyOrder_BaseCurrencyAmountToBuy?.ToStringWithAsset());
                }

                if (result.BuyQuoteCurrencyLimitPrice != null)
                {
                    arbitrageCtx.BuyOrder_QuoteCurrencyLimitPriceToUse = result.BuyQuoteCurrencyLimitPrice;
                    m_logger.Info("\toverride QuoteCurrencyLimitPriceToUse = {0}", arbitrageCtx.BuyOrder_QuoteCurrencyLimitPriceToUse?.ToStringWithAsset());
                }

                m_logger.Info("\tarbitrating...");
                arbitrageCtx.BreakOnState = null;
                await m_arbitrager.Arbitrage(arbitrageCtx);

                if (arbitrageCtx.Error != null)
                {
                    m_logger.Info("\tarbitrage failed!");
                }
                else
                {
                    m_logger.Info("\tarbitrage finished!");
                    string finalResult =
                        string.Format("\tARBITRAGE RESULT {12} -> {13}\n\t\t\t(Profit       ) {0}, {1}\n\t\t\t(Buy          ) {2} -> {3}\n\t\t\t(Sell         ) {4} -> {5}\n\t\t\t(BuyerBalance ) {6}, {7}\n\t\t\t(SellerBalance) {8}, {9}\n\t\t\t(TotalBalance ) {10}, {11}",
                        arbitrageCtx.FinishedResult.ProfitPercentage,
                        arbitrageCtx.FinishedResult.QuoteCurrencyDelta.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.QuoteCurrencySpent.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.BaseCurrencyBought.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.BaseCurrencySold.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.QuoteCurrencyEarned.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.BuyerBalance.QuoteCurrency.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.BuyerBalance.BaseCurrency.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.SellerBalance.QuoteCurrency.ToStringWithAsset(),
                        arbitrageCtx.FinishedResult.SellerBalance.BaseCurrency.ToStringWithAsset(),
                        (arbitrageCtx.FinishedResult.BuyerBalance.QuoteCurrency + arbitrageCtx.FinishedResult.SellerBalance.QuoteCurrency).ToStringWithAsset(),
                        (arbitrageCtx.FinishedResult.BuyerBalance.BaseCurrency + arbitrageCtx.FinishedResult.SellerBalance.BaseCurrency).ToStringWithAsset(),
                        arbitrageCtx.Buyer.Name,
                        arbitrageCtx.Seller.Name);

                    m_logger.Info(finalResult);
                }                
            }
        }

        protected virtual async Task<ShouldDoArbitrageResult> ShouldDoArbitrage(TContext mgrCtx, ArbitrageInfo info)
        {
            await Task.Delay(0);

            var percentage = info.ProfitCalculation.ProfitPercentage;
            if (percentage < PercentageValue.FromPercentage(0.6m))
            {
                return new ShouldDoArbitrageResult()
                {
                    DoArbitrage = false,
                    Reason = string.Format("Profit {0} is too small (< 0.6%)", percentage)
                };
            }

            if (info.MaxBaseCurrencyAmountToArbitrage < 0.05m)
            {
                return new ShouldDoArbitrageResult()
                {
                    DoArbitrage = false,
                    Reason = string.Format("{0} amount {1} is too small (< 0.05)", info.AssetPair.Base, info.MaxBaseCurrencyAmountToArbitrage)
                };
            }

            return new ShouldDoArbitrageResult()
            {
                DoArbitrage = true,
                Reason = string.Format("Profit {0} > 0.6% and {1} amount > 0.05", percentage, info.AssetPair.Base),
                ArbitrageInfo = info
            };
        }

        protected class ShouldDoArbitrageResult
        {
            public ArbitrageInfo ArbitrageInfo { get; set; }
            public bool DoArbitrage { get; set; }
            public string Reason { get; set; }

            public PriceValue? BuyQuoteCurrencyLimitPrice { get; set; }
            public PriceValue? BuyBaseCurrencyVolume { get; set; }
        }
    }

    public class DefaultArbitrageManager : ArbitrageManagerBase<ArbitrageManagerContext>
    {
        public DefaultArbitrageManager(IArbitrager arbitrager, ILogger logger, IClock clock = null)
            : base(arbitrager, logger, clock, () => new ArbitrageManagerContext(clock, logger))
        {
        }
    }

    public class ArbitrageManagerContext
    {
        public IClock Clock { get; set; }
        public ILogger Logger { get; set; }
        public DateTime LastAction { get; set; }

        public TimeSpan GetTimeSinceLastAction() => Clock.UtcNow - LastAction;

        public ArbitrageManagerContext(IClock clock, ILogger logger)
        {
            Clock = clock;
            Logger = logger;
            LastAction = DateTime.MinValue;
        }
    }
}
