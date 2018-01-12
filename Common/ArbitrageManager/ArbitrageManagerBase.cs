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
            if (timeSinceLastAction > TimeSpan.FromMinutes(1))
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
            decimal chunkEur = 100;
            var arbitrageCtx = ArbitrageContext.GetInfo(chunkEur);
            arbitrageCtx.AbortIfFiatToSpendIsMoreThanBalance = false;
            await m_arbitrager.Arbitrage(arbitrageCtx);

            string msg = string.Format("\tArbitrageInfo: Profit {0:0.#}%, {1:0.##} EUR | (Bid) {2:0.##} EUR - (Ask) {3:0.##} EUR = (Spread) {4:0.##} EUR | (Orders) {5:0.##} EUR -> {6:0.####} ETH -> {7:0.##} EUR | (Balance) {8:0.##} EUR, {9:0.####} ETH | (Chunk) {10:0.##} EUR",
                arbitrageCtx.Info.MaxProfitPercentage * 100,
                arbitrageCtx.Info.MaxEurProfit,
                arbitrageCtx.Info.BestSellPrice,
                arbitrageCtx.Info.BestBuyPrice,
                arbitrageCtx.Info.MaxNegativeSpreadEur,
                arbitrageCtx.Info.MaxEursToSpend,
                arbitrageCtx.Info.MaxEthAmountToArbitrage,
                arbitrageCtx.Info.MaxEursToEarn,
                arbitrageCtx.Info.EurBalance,
                arbitrageCtx.Info.EthBalance,
                chunkEur);

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
                if (result.EthToBuy != null)
                {
                    arbitrageCtx.BuyOrder_EthAmountToBuy = result.EthToBuy;
                    m_logger.Info("\toverride EthAmountToBuy = {0:0.####} ETH", arbitrageCtx.BuyOrder_EthAmountToBuy);
                }

                if (result.BuyLimitPrice != null)
                {
                    arbitrageCtx.BuyOrder_LimitPriceToUse = result.BuyLimitPrice;
                    m_logger.Info("\toverride BuyLimitPrice = {0:0.##} EUR", arbitrageCtx.BuyOrder_LimitPriceToUse);
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
                        string.Format("\tProfit {0:0.#}%, {1:0.##} EUR | (Buy) {2:0.##} EUR -> {3:0.####} ETH  | (Sell) {4:0.####} ETH -> {5:0.##} EUR | (BuyerBalance) {6:0.##} EUR, {7:0.####} ETH | (SellerBalance) {8:0.##} EUR, {9:0.####} ETH",
                        arbitrageCtx.FinishedResult.Profit * 100,
                        arbitrageCtx.FinishedResult.FiatDelta,
                        arbitrageCtx.FinishedResult.FiatSpent,
                        arbitrageCtx.FinishedResult.EthBought,
                        arbitrageCtx.FinishedResult.EthSold,
                        arbitrageCtx.FinishedResult.FiatEarned,
                        arbitrageCtx.FinishedResult.BuyerBalance.Eur,
                        arbitrageCtx.FinishedResult.BuyerBalance.Eth,
                        arbitrageCtx.FinishedResult.SellerBalance.Eur,
                        arbitrageCtx.FinishedResult.SellerBalance.Eth);

                    m_logger.Info(finalResult);
                }                
            }
        }

        protected virtual async Task<ShouldDoArbitrageResult> ShouldDoArbitrage(TContext mgrCtx, ArbitrageInfo info)
        {
            await Task.Delay(0);

            var percentage = info.ProfitCalculation.ProfitPercentage * 100;
            if (percentage > 2m)
            {
                return new ShouldDoArbitrageResult()
                {
                    DoArbitrage = true,
                    Reason = string.Format("Profit {0:0.##}% > 2%", percentage),
                    ArbitrageInfo = info
                };
            }

            return new ShouldDoArbitrageResult()
            {
                DoArbitrage = false,
                Reason = string.Format("Profit {0:0.##}% is too small", percentage)
            };
        }

        protected class ShouldDoArbitrageResult
        {
            public ArbitrageInfo ArbitrageInfo { get; set; }
            public bool DoArbitrage { get; set; }
            public string Reason { get; set; }

            public decimal? BuyLimitPrice { get; set; }
            public decimal? EthToBuy { get; set; }
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
