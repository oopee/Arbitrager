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
        IProfitCalculator m_profitCalculator;
        ILogger m_logger;
        IDatabaseAccess m_dataAccess;

        public decimal ChunkEur { get; set; } = 2000m;

        public List<IExchange> Exchanges { get; set; } = new List<IExchange>();
        IEnumerable<IExchange> IArbitrager.Exchanges => Exchanges;

        public IProfitCalculator ProfitCalculator => m_profitCalculator;

        public static int MaxBuyRetries => 10;
        public static TimeSpan MaxBuyTotalTime => TimeSpan.FromSeconds(30);

        public static int MaxSellRetries => 10;
        public static TimeSpan MaxSellTotalTime => TimeSpan.FromSeconds(30);


        public event EventHandler<ArbitrageContext> StateChanged;

        public DefaultArbitrager(
            IEnumerable<IExchange> exhanges,
            IProfitCalculator profitCalculator,
            IDatabaseAccess dataAccess,
            ILogger logger)
        {
            Exchanges = exhanges.ToList();
            m_profitCalculator = profitCalculator;
            m_logger = logger.WithName(GetType().Name);
            m_dataAccess = dataAccess;
        }

        public async Task<AccountsInfo> GetAccountsInfo()
        {
            m_logger.Debug("GetAccountsInfo()");

            var result = await ForAllExchanges(exchange => GetAccountInfo(exchange));

            return new AccountsInfo()
            {
                Accounts = result
            };
        }

        private async Task<List<T>> ForAllExchanges<T>(Func<IExchange, Task<T>> action)
        {
            List<Task<T>> tasks = new List<Task<T>>();
            foreach (var exchange in Exchanges)
            {
                tasks.Add(action(exchange));
            }

            await Task.WhenAll(tasks);

            return tasks.Select(x => x.Result).ToList();
        }

        private async Task<AccountInfo> GetAccountInfo(IExchange exchange)
        {
            m_logger.Debug("GetAccountInfo()");
            m_logger.Debug("Calling GetCurrentBalance({0})", exchange?.Name);
            var balance = await exchange.GetCurrentBalance();

            m_logger.Debug("Calling GetPaymentMethods({0})", exchange?.Name);
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
            m_logger.Debug("GetStatus(includeBalance: {0})", includeBalance);

            var result = await ForAllExchanges(async exchange =>
            {
                BalanceResult balance = null;
                if (includeBalance)
                {
                    m_logger.Debug("Calling GetCurrentBalance({0})", exchange.Name);
                    balance = await exchange.GetCurrentBalance();
                }

                m_logger.Debug("Calling GetOrderBook({0})", exchange.Name);
                var orderBook = await exchange.GetOrderBook();
                return new ExchangeStatus()
                {
                    Balance = balance,
                    Exchange = exchange,
                    MakerFee = exchange.MakerFeePercentage,
                    TakerFee = exchange.TakerFeePercentage,
                    OrderBook = orderBook
                };
            });

            return new Status()
            {
                Exchanges = result
            };
        }

        public async Task<ArbitrageContext> Arbitrage(ArbitrageContext ctx)
        {
            var logger = m_logger.WithAppendName("Arbitrage");
            logger.Debug("Starting from state {0}", ctx.State);

            if (ctx.Error != null)
            {
                throw new InvalidOperationException("Arbitrage() called when context in in error state");
            }

            ctx.Logger = logger;

            while (ctx.State != ArbitrageState.Finished)
            {
                logger.Debug("Handling state {0}", ctx.State);

                await OnStateBegin(ctx);

                ArbitrageState newState;
                switch (ctx.State)
                {
                    case ArbitrageState.NotStarted:
                        newState = ArbitrageState.CheckStatus;
                        break;

                    case ArbitrageState.CheckStatus:
                        await DoArbitrage_CheckStatus(ctx);
                        newState = ArbitrageState.PlaceBuyOrder;
                        break;

                    case ArbitrageState.PlaceBuyOrder:
                        await DoArbitrage_PlaceBuyOrder(ctx);
                        newState = ArbitrageState.GetBuyOrderInfo;
                        break;

                    case ArbitrageState.GetBuyOrderInfo:
                        await DoArbitrage_GetBuyOrderInfo(ctx);
                        newState = ArbitrageState.PlaceSellOrder;
                        break;

                    case ArbitrageState.PlaceSellOrder:
                        await DoArbitrage_PlaceSellOrder(ctx);
                        newState = ArbitrageState.GetSellOrderInfo;
                        break;

                    case ArbitrageState.GetSellOrderInfo:
                        await DoArbitrage_GetSellOrderInfo(ctx);
                        newState = ArbitrageState.CalculateFinalResult;
                        break;

                    case ArbitrageState.CalculateFinalResult:
                        await DoArbitrage_CalculateFinalResult(ctx);
                        newState = ArbitrageState.Finished;
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("State '{0}' not handled", ctx.State));
                }
                
                await OnStateEnd(ctx);

                if (ctx.Error != null)
                {
                    logger.Debug("\tstate {0} did not finish succesfully. Error: {1}", ctx.State, ctx.Error);
                    logger.Debug("\taborting!");
                    return ctx;
                }

                logger.Debug("\tmoving to next state {0}", newState);
                ctx.State = newState;

                if (ctx.BreakOnState == ctx.State)
                {
                    logger.Debug("\tbreaking execution...");
                    return ctx;
                }
            }

            logger.Debug("\tfinished!");
            await OnStateEnd(ctx);
            return ctx;
        }

        protected virtual async Task DoArbitrage_CheckStatus(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_CheckStatus");

            // Calculate arbitrage info
            var info = await GetInfoForArbitrage(
                maxFiatToSpend: ctx.SpendWholeBalance ? PriceValue.FromEUR(decimal.MaxValue) : ctx.UserFiatToSpend,
                fiatOptions: BalanceOption.CapToBalance,
                maxEthToSpend: PriceValue.FromETH(decimal.MaxValue),
                ethOptions: BalanceOption.CapToBalance);

            if (!ctx.SpendWholeBalance && ctx.UserFiatToSpend > info.EurBalance)
            {
                // Explicit EUR amount was specified but it is more than we have at exchange B -> abort
                if (ctx.AbortIfFiatToSpendIsMoreThanBalance)
                {
                    // NOTE: we could simply ignore this check and go on. In that case if user specified too large EUR amount his whole balance would be used.
                    //       As we don't want to guess if the user did this on purpose or if it was a typo, it is better to abort and let the user try again
                    ctx.Error = ArbitrageError.InvalidBalance;
                    return;
                }
                else
                {
                    ctx.UserFiatToSpend = info.EurBalance;
                }
            }

            if (!info.IsEurBalanceSufficient || !info.IsEthBalanceSufficient)
            {
                // We don't have enough EUR or ETH
                ctx.Error = ArbitrageError.InvalidBalance;
            }

            ctx.Info = info;
            ctx.BuyOrder_LimitPriceToUse = ctx.Info.BuyLimitPricePerUnit;
            ctx.BuyOrder_EthAmountToBuy = ctx.Info.MaxEthAmountToArbitrage;
            ctx.Buyer = info.Buyer.Exchange;
            ctx.Seller = info.Seller.Exchange;
        }

        protected virtual async Task DoArbitrage_PlaceBuyOrder(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_PlaceBuyOrder");

            // Check that state is valid
            if (ctx.BuyOrderId != null)
            {
                throw new InvalidOperationException(string.Format("DoArbitrage_PlaceBuyOrder: BuyOrderId already set! ({0})", ctx.BuyOrderId));
            }

            if (ctx.BuyOrder_LimitPriceToUse == null)
            {
                throw new InvalidOperationException("DoArbitrage_PlaceBuyOrder: BuyOrder_LimitPriceToUse is null");
            }

            if (ctx.BuyOrder_EthAmountToBuy == null)
            {
                throw new InvalidOperationException("DoArbitrage_PlaceBuyOrder: BuyOrder_EthAmountToBuy is null");
            }

            // Place order
            var buyLimitPricePerUnit = ctx.BuyOrder_LimitPriceToUse.Value;
            var maxEthToBuy = ctx.BuyOrder_EthAmountToBuy.Value;

            logger.Debug("Placing buy order (limit: {0} EUR, volume: {1} ETH)", buyLimitPricePerUnit, maxEthToBuy);
            var buyOrderId = await TryPlaceBuyOrder(ctx.Buyer, buyLimitPricePerUnit, maxEthToBuy, logger);

            // Handle result
            if (buyOrderId == null)
            {
                logger.Error("\tbuy order could not be placed. Aborting...");
                ctx.Error = ArbitrageError.CouldNotPlaceBuyOrder;
            }
            else
            {
                logger.Debug("\tbuy order placed (orderId: {0})", buyOrderId);
                ctx.BuyOrderId = buyOrderId;
            }            
        }

        protected async Task<OrderId?> TryPlaceBuyOrder(IExchange buyer, PriceValue buyLimitPricePerUnit, PriceValue maxEthToBuy, ILogger logger)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            int i = 0;
            for (; i < MaxBuyRetries && sw.Elapsed < MaxBuyTotalTime; ++i)
            {
                var startTime = TimeService.UtcNow;
                try
                {
                    var priceLimit = buyLimitPricePerUnit.Round();
                    var ethToBuy = maxEthToBuy.Round(decimalPlaces: 5);
                    var buyOrder = await buyer.PlaceImmediateBuyOrder(priceLimit, ethToBuy);
                    return buyOrder.Id;
                }
                catch (Exception e)
                {
                    if (e is ArgumentException)
                    {
                        throw;
                    }

                    if (buyer.CanGetClosedOrders)
                    {
                        logger.Error("\tError! Reason: {0}", e.Message);
                        logger.Error("\ttry to determine if order was placed or not... (wait for a while first)");
                        await Task.Delay(2000);
                        logger.Error("\t\tget closed orders...");
                        var closedOrders = await buyer.GetClosedOrders(new GetOrderArgs() { StartUtc = startTime });
                        var order = closedOrders.Where(x => FuzzyCompare(x.Volume, maxEthToBuy) && x.OpenTime >= startTime).FirstOrDefault();
                        if (order != null)
                        {
                            logger.Error("\t\tfound recently closed order with same volume! Volume: {0}, CreatedTime: {1}, State: {2}, Id: {3}", order.Volume, order.OpenTime, order.State, order.Id);
                            // Buy order was successful
                            return order.Id;
                        }
                    }
                }

                logger.Info("\tretrying...");
            }

            logger.Error("\tmaxretry count (retires: {0}) or max time (time: {1:N2}s) reached, aborting...", i, sw.Elapsed.TotalSeconds);
            return null;
        }

        protected async Task<OrderId?> TryPlaceSellOrder(IExchange seller, PriceValue sellLimitPricePerUnit, PriceValue maxEthToBuy, ILogger logger)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            int i = 0;
            for (; i < MaxSellRetries && sw.Elapsed < MaxSellTotalTime; ++i)
            {
                var startTime = TimeService.UtcNow;
                try
                {
                    var priceLimit = sellLimitPricePerUnit.Round();
                    var ethToBuy = maxEthToBuy.Round(decimalPlaces: 5);
                    var sellOrder = await seller.PlaceImmediateSellOrder(priceLimit, ethToBuy);
                    return sellOrder.Id;
                }
                catch (Exception e)
                {
                    if (e is ArgumentException)
                    {
                        throw;
                    }

                    if (seller.CanGetClosedOrders)
                    {
                        logger.Error("\tError! Reason: {0}", e.Message);
                        logger.Error("\ttry to determine if order was placed or not... (wait for a while first)");
                        await Task.Delay(2000);
                        logger.Error("\t\tget closed orders...");
                        var closedOrders = await seller.GetClosedOrders(new GetOrderArgs() { StartUtc = startTime });
                        var order = closedOrders.Where(x => FuzzyCompare(x.Volume, maxEthToBuy) && x.OpenTime >= startTime).FirstOrDefault();
                        if (order != null)
                        {
                            logger.Error("\t\tfound recently closed order with same volume! Volume: {0}, CreatedTime: {1}, State: {2}, Id: {3}", order.Volume, order.OpenTime, order.State, order.Id);
                            // Buy order was successful
                            return order.Id;
                        }
                    }
                }

                logger.Info("\tretrying...");
            }

            logger.Error("\tmaxretry count (retires: {0}) or max time (time: {1:N2}s) reached, aborting...", i, sw.Elapsed.TotalSeconds);
            return null;
        }

        private static bool FuzzyCompare(PriceValue a, PriceValue b)
        {
            return Math.Abs(a.Value - b.Value) < 0.01m;
        }

        protected virtual async Task DoArbitrage_GetBuyOrderInfo(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_GetBuyOrderInfo");

            if (ctx.BuyOrderId == null)
            {
                throw new InvalidOperationException("DoArbitrage_GetBuyOrderInfo: BuyOrderId has not been set!");
            }

            logger.Debug("Getting buy order info (orderId: {0})", ctx.BuyOrderId);
            var buyOrderInfo = await ctx.Buyer.GetOrderInfo(ctx.BuyOrderId.Value);
            ctx.BuyOrder = buyOrderInfo;
            ctx.BuyEthAmount = buyOrderInfo.FilledVolume;
            logger.Debug("\tgot buy order info (filledVolume: {0}, cost: {1}, state: {2})", ctx.BuyEthAmount, buyOrderInfo.CostIncludingFee, buyOrderInfo.State);

            if (buyOrderInfo.State == OrderState.Open)
            {
                // TODO: what to do?
                logger.Error("\tbuy order is still OPEN!");
            }

            if (ctx.BuyEthAmount <= 0m)
            {
                // we couldn't by any eth -> abort
                ctx.Error = ArbitrageError.ZeroEthBought;
            }

            await m_dataAccess.StoreTransaction(buyOrderInfo);
        }

        protected virtual async Task DoArbitrage_PlaceSellOrder(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_PlaceSellOrder");

            if (ctx.BuyEthAmount == null || ctx.BuyEthAmount <= 0m)
            {
                throw new InvalidOperationException("DoArbitrage_PlaceSellOrder: BuyEthAmount has not been set!");
            }

            var ethToSell = ctx.BuyEthAmount.Value.Round(decimalPlaces: 5);
            var sellLimitPrice = (ctx.Info.EstimatedAvgBuyUnitPrice * 0.9m).Round();

            logger.Debug("Placing sell order (volume: {0} ETH, sell limit price: {1} EUR)", ethToSell, sellLimitPrice);
            var sellOrderId = await TryPlaceSellOrder(ctx.Seller, sellLimitPrice, ethToSell, logger);

            // Handle result
            if (sellOrderId == null)
            {
                logger.Error("\tsell order could not be placed. Aborting...");
                ctx.Error = ArbitrageError.CouldNotPlaceSellOrder;
            }
            else
            {
                logger.Debug("\tsell order placed (orderId: {0})", sellOrderId);
                ctx.BuyOrderId = sellOrderId;
            }

            ctx.SellOrderId = sellOrderId;
        }

        protected virtual async Task DoArbitrage_GetSellOrderInfo(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_GetSellOrderInfo");

            if (ctx.SellOrderId == null)
            {
                throw new InvalidOperationException("DoArbitrage_GetSellOrderInfo: SellOrderId has not been set!");
            }

            logger.Debug("Getting sell order info (orderId: {0})", ctx.SellOrderId);
            var sellOrderInfo = await ctx.Seller.GetOrderInfo(ctx.SellOrderId.Value);
            ctx.SellOrder = sellOrderInfo;
            logger.Debug("\tgot buy sell info (filledVolume: {0}, cost: {1}, state: {2})", sellOrderInfo.FilledVolume, sellOrderInfo.CostIncludingFee, sellOrderInfo.State);

            await m_dataAccess.StoreTransaction(sellOrderInfo);
        }

        protected virtual async Task DoArbitrage_CalculateFinalResult(ArbitrageContext ctx)
        {
            if (ctx.SellOrder == null)
            {
                throw new InvalidOperationException("DoArbitrage_CalculateFinalResult: SellOrder has not been set!");
            }

            if (ctx.BuyOrder == null)
            {
                throw new InvalidOperationException("DoArbitrage_CalculateFinalResult: BuyOrder has not been set!");
            }

            var buyerBalanceTask = ctx.Buyer.GetCurrentBalance();
            var sellerBalanceTask = ctx.Seller.GetCurrentBalance();

            await buyerBalanceTask;
            await sellerBalanceTask;

            ctx.FinishedResult = new ArbitrageContext.FinishedResultData()
            {
                EthBought = ctx.BuyOrder.FilledVolume,
                EthSold = ctx.SellOrder.FilledVolume,
                FiatSpent = ctx.BuyOrder.CostIncludingFee,
                FiatEarned = ctx.SellOrder.CostIncludingFee,
                BuyerBalance = buyerBalanceTask.Result,
                SellerBalance = sellerBalanceTask.Result
            };

            m_logger.Debug("\tfinal result: {0}", ctx.FinishedResult);
            if (ctx.FinishedResult.EthDelta != 0)
            {
                m_logger.Debug("\tWARNING! All ETH could not be sold! Remaining eth: {0} ETH", ctx.FinishedResult.EthDelta);
            }

            if (ctx.FinishedResult.FiatDelta < 0)
            {
                m_logger.Debug("\tWARNING! Negative profit: {0} EUR", ctx.FinishedResult.FiatDelta);
            }
        }

        protected virtual Task OnStateBegin(ArbitrageContext ctx)
        {            
            return Task.CompletedTask;
        }

        protected virtual Task OnStateEnd(ArbitrageContext ctx)
        {
            OnStateChanged(ctx);
            return Task.CompletedTask;
        }

        public async Task<ArbitrageInfo> GetInfoForArbitrage(PriceValue maxFiatToSpend, BalanceOption fiatOptions, PriceValue maxEthToSpend, BalanceOption ethOptions)
        {
            // Get current prices, balances etc
            var status = await GetStatus(true);

            var r1 = GetInfoForArbitrage(status.Exchanges[0], status.Exchanges[1], maxFiatToSpend, fiatOptions, maxEthToSpend, ethOptions);
            var r2 = GetInfoForArbitrage(status.Exchanges[1], status.Exchanges[0], maxFiatToSpend, fiatOptions, maxEthToSpend, ethOptions);

            return r1.MaxProfitPercentage > r2.MaxProfitPercentage ? r1 : r2;
        }

        public ArbitrageInfo GetInfoForArbitrage(ExchangeStatus buyer, ExchangeStatus seller, PriceValue maxFiatToSpend, BalanceOption fiatOptions, PriceValue maxEthToSpend, BalanceOption ethOptions)
        {
            if (fiatOptions == BalanceOption.CapToBalance)
            {
                maxFiatToSpend = PriceValue.Min(maxFiatToSpend, buyer.Balance.Eur);
            }

            if (ethOptions == BalanceOption.CapToBalance)
            {
                maxEthToSpend = PriceValue.Min(maxEthToSpend, seller.Balance.Eth);
            }

            // Calculate estimated profit based on prices/balances/etc
            var calc = m_profitCalculator.CalculateProfit(buyer, seller, maxFiatToSpend, maxEthToSpend);

            ArbitrageInfo info = new ArbitrageInfo()
            {
                Buyer = buyer,
                Seller = seller,
                ProfitCalculation = calc,
                IsProfitable = calc.ProfitPercentage >= PercentageValue.FromPercentage(2) // 2% threshold
            };

            return info;
        }

        protected virtual void OnStateChanged(ArbitrageContext ctx)
        {
            StateChanged?.Invoke(this, ctx);
        }
    }
}
