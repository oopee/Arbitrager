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

        public DefaultArbitrager(
            IBuyer buyer,
            ISeller seller,
            IProfitCalculator profitCalculator,
            IDatabaseAccess dataAccess,
            ILogger logger)
        {
            m_buyer = buyer;
            m_seller = seller;
            m_profitCalculator = profitCalculator;
            m_logger = logger.WithName(GetType().Name);
            m_dataAccess = dataAccess;
        }

        public async Task<AccountsInfo> GetAccountsInfo()
        {
            m_logger.Debug("GetAccountsInfo()");
            var buyer = await GetAccountInfo(m_buyer);
            var seller = await GetAccountInfo(m_seller);

            return new AccountsInfo()
            {
                Accounts = new List<AccountInfo>() { buyer, seller }
            };
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
            BalanceResult buyerBalance = null;
            IAskOrderBook askOrderBook = null;

            BalanceResult sellerBalance = null;
            IBidOrderBook bidOrderBook = null;

            Func<Task> buyerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    m_logger.Debug("Calling GetCurrentBalance({0})", m_buyer.Name);
                    buyerBalance = await m_buyer.GetCurrentBalance();
                }

                m_logger.Debug("Calling GetAsks({0})", m_buyer.Name);
                askOrderBook = await m_buyer.GetAsks();
            };

            Func<Task> sellerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    m_logger.Debug("Calling GetCurrentBalance({0})", m_seller.Name);
                    sellerBalance = await m_seller.GetCurrentBalance();
                }

                m_logger.Debug("Calling GetBids({0})", m_seller.Name);
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

        public async Task Arbitrage(ArbitrageContext ctx)
        {
            var logger = m_logger.WithAppendName("Arbitrage");
            logger.Debug("Starting from state {0}", ctx.State);

            if (ctx.Error != null)
            {
                throw new InvalidOperationException("Arbitrage() called when context in in error state");
            }

            ctx.Logger = logger;

            while (true)
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
                        newState = ArbitrageState.WithdrawFiat;
                        break;

                    case ArbitrageState.WithdrawFiat:
                        await DoArbitrage_WithdrawFiat(ctx);
                        newState = ArbitrageState.TransferEth;
                        break;

                    case ArbitrageState.TransferEth:
                        await DoArbitrage_TransferEth(ctx);
                        newState = ArbitrageState.Finished;
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("State '{0}' not handled", ctx.State));
                }

                if (ctx.Error != null)
                {
                    logger.Debug("\tstate {0} did not finish succesfully. Error: {1}", ctx.State, ctx.Error);
                    logger.Debug("\taborting!");
                    return;
                }

                await OnStateEnd(ctx);

                logger.Debug("\tmoving to next state {0}", newState);
                ctx.State = newState;
            }
        }

        protected virtual async Task DoArbitrage_CheckStatus(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_CheckStatus");

            // Calculate arbitrage info
            var info = await GetInfoForArbitrage(
                maxFiatToSpend: ctx.SpendWholeBalance ? decimal.MaxValue : ctx.UserFiatToSpend,
                fiatOptions: BalanceOption.CapToBalance,
                maxEthToSpend: decimal.MaxValue,
                ethOptions: BalanceOption.CapToBalance);

            if (!ctx.SpendWholeBalance && ctx.UserFiatToSpend > info.EurBalance)
            {
                // Explicit EUR amount was specified but it is more than we have at exchange B -> abort
                // NOTE: we could simply ignore this check and go on. In that case if user specified too large EUR amount his whole balance would be used.
                //       As we don't want to guess if the user did this on purpose or if it was a typo, it is better to abort and let the user try again
                ctx.Error = ArbitrageError.InvalidBalance;
                return;
            }

            if (!info.IsEurBalanceSufficient || !info.IsEthBalanceSufficient)
            {
                // We don't have enough EUR or ETH
                ctx.Error = ArbitrageError.InvalidBalance;
            }

            ctx.Info = info;
        }

        protected virtual async Task DoArbitrage_PlaceBuyOrder(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_PlaceBuyOrder");

            if (ctx.BuyOrderId != null)
            {
                throw new InvalidOperationException(string.Format("DoArbitrage_PlaceBuyOrder: BuyOrderId already set! ({0})", ctx.BuyOrderId));
            }

            var buyLimitPricePerUnit = ctx.Info.BuyLimitPricePerUnit;
            var maxEthToBuy = ctx.Info.MaxEthAmountToArbitrage;

            logger.Debug("Placing buy order (limit: {0} EUR, volume: {1} ETH)", buyLimitPricePerUnit, maxEthToBuy);
            var buyOrder = await Buyer.PlaceImmediateBuyOrder(buyLimitPricePerUnit, maxEthToBuy);
            logger.Debug("\tbuy order placed (orderId: {0})", buyOrder.Id);

            ctx.BuyOrderId = buyOrder.Id;
        }

        protected virtual async Task DoArbitrage_GetBuyOrderInfo(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_GetBuyOrderInfo");

            if (ctx.BuyOrderId != null)
            {
                throw new InvalidOperationException("DoArbitrage_GetBuyOrderInfo: BuyOrderId has not been set!");
            }

            logger.Debug("Getting buy order info (orderId: {0})", ctx.BuyOrderId);
            var buyOrderInfo = await Buyer.GetOrderInfo(ctx.BuyOrderId.Value);
            ctx.BuyEthAmount = buyOrderInfo.FilledVolume;
            logger.Debug("\tgot buy order info (filledVolume: {0}, cost: {1}, state: {2})", ctx.BuyEthAmount, buyOrderInfo.Cost, buyOrderInfo.State);

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
        }

        protected virtual async Task DoArbitrage_PlaceSellOrder(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_PlaceSellOrder");

            if (ctx.BuyEthAmount == null || ctx.BuyEthAmount <= 0m)
            {
                throw new InvalidOperationException("DoArbitrage_PlaceSellOrder: BuyEthAmount has not been set!");
            }

            var ethToSell = ctx.BuyEthAmount.Value;

            logger.Debug("Placing sell order (volume: {0} ETH)", ethToSell);
            var sellOrder = await Seller.PlaceMarketSellOrder(ethToSell);
            logger.Debug("\tsell order placed (orderId: {0})", sellOrder.Id);

            ctx.SellOrderId = sellOrder.Id;
        }

        protected virtual async Task DoArbitrage_GetSellOrderInfo(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_GetSellOrderInfo");

            if (ctx.SellOrderId != null)
            {
                throw new InvalidOperationException("DoArbitrage_GetSellOrderInfo: SellOrderId has not been set!");
            }

            logger.Debug("Getting sell order info (orderId: {0})", ctx.SellOrderId);
            var sellOrderInfo = await Seller.GetOrderInfo(ctx.SellOrderId.Value);
            logger.Debug("\tgot buy sell info (filledVolume: {0}, cost: {1}, state: {2})", sellOrderInfo.FilledVolume, sellOrderInfo.Cost, sellOrderInfo.State);

            // TODO!
        }

        protected virtual async Task DoArbitrage_WithdrawFiat(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_WithdrawFiat");
            logger.Debug("Not implemented");

            await Task.Delay(0);

            // throw new NotImplementedException();
        }

        protected virtual async Task DoArbitrage_TransferEth(ArbitrageContext ctx)
        {
            var logger = ctx.Logger.WithName(GetType().Name, "DoArbitrage_TransferEth");
            logger.Debug("Not implemented");

            await Task.Delay(0);

            // throw new NotImplementedException();
        }

        protected virtual Task OnStateBegin(ArbitrageContext ctx)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnStateEnd(ArbitrageContext ctx)
        {
            return Task.CompletedTask;
        }

        public async Task<ArbitrageInfo> GetInfoForArbitrage(decimal maxFiatToSpend, BalanceOption fiatOptions, decimal maxEthToSpend, BalanceOption ethOptions)
        {
            // Get current prices, balances etc
            var status = await GetStatus(true);

            if (fiatOptions == BalanceOption.CapToBalance)
            {
                maxFiatToSpend = Math.Min(maxFiatToSpend, status.Buyer.Balance.Eur);
            }

            if (ethOptions == BalanceOption.CapToBalance)
            {
                maxEthToSpend = Math.Min(maxEthToSpend, status.Seller.Balance.Eth);
            }

            // Calculate estimated profit based on prices/balances/etc
            var calc = m_profitCalculator.CalculateProfit(status.Buyer, status.Seller, maxFiatToSpend, maxEthToSpend);

            ArbitrageInfo info = new ArbitrageInfo()
            {
                Status = status,
                ProfitCalculation = calc,
                IsProfitable = calc.ProfitPercentage >= 0.02m // 2% threshold
            };

            return info;
        }
    }
}
