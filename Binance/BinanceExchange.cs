using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.API.Csharp.Client.Models.Account;
using Interface;

namespace Binance
{
    public class BinanceExchange : Interface.IExchange
    {
        Binance.API.Csharp.Client.BinanceClient m_client;
        ILogger m_logger;

        public BinanceConfiguration Configuration { get; private set; }        
        public bool CanGetClosedOrders => true; // TODO mikko
        public string Name => "Binance";
        public PercentageValue TakerFeePercentage => PercentageValue.FromPercentage(0.1m); // 0.1%
        public PercentageValue MakerFeePercentage => PercentageValue.FromPercentage(0.1m); // 0.1%

        public BinanceExchange(BinanceConfiguration configuration, ILogger logger)
        {
            Configuration = configuration;

            m_logger = logger.WithName(GetType().Name);
            
            var apiClient = new Binance.API.Csharp.Client.ApiClient(configuration.Key, configuration.Secret);
            m_client = new Binance.API.Csharp.Client.BinanceClient(apiClient);
        }

        public async Task<BalanceResult> GetCurrentBalance(AssetPair assetPair)
        {
            var accountInfo = await m_client.GetAccountInfo(10000);

            // TODO mikko onko ok ottaa vain free, miten locked?
            var all = accountInfo.Balances.ToDictionary(x => x.Asset, x => x.Free);

            var binanceQuote = assetPair.Quote.ToString();
            var binanceBase = assetPair.Base.ToString();

            return new BalanceResult()
            {
                AssetPair = assetPair,
                All = all,
                QuoteCurrency = new PriceValue(all.Where(x => x.Key == binanceQuote).FirstOrDefault().Value, assetPair.Quote),
                BaseCurrency = new PriceValue(all.Where(x => x.Key == binanceBase).FirstOrDefault().Value, assetPair.Base),
            };
        }

        public async Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            var result = await m_client.GetOrderBook(assetPair.ShortName);

            var orderBook = new OrderBook()
            {
                AssetPair = assetPair
            };

            if (result.Asks.Any())
            {
                orderBook.Asks.AddRange(result.Asks.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.Price, assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Quantity, assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            if (result.Bids.Any())
            {
                orderBook.Bids.AddRange(result.Bids.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.Price, assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Quantity, assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            return orderBook;
        }

        public Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FullOrder>> GetClosedOrders2(GetOrderArgs args)
        {
            Guard.IsTrue(args.StartUtc == null && args.EndUtc == null, "BinanceExchange does not support start/end times");

            var allOrders = await m_client.GetAllOrders(args.AssetPair.ShortName);
            var result = allOrders.Select(o => ParseOrder(o, args.AssetPair)).Where(o => o.State == OrderState.Closed).ToList();
            return result;
        }

        public Task<List<FullOrder>> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public async Task<List<FullOrder>> GetOpenOrders2(AssetPair assetPair)
        {
            var orders = await m_client.GetCurrentOpenOrders(assetPair.ShortName);
            var result = orders.Select(x => ParseOrder(x, assetPair)).ToList();
            return result;
        }

        private FullOrder ParseOrder(Order binanceOrder, AssetPair assetPair)
        {
            OrderSide side = ParseSide(binanceOrder.Side);
            
            return new FullOrder()
            {
                Exchange = Name,
                Id = new OrderId(binanceOrder.ClientOrderId),
                BaseAsset = assetPair.Base,
                QuoteAsset = assetPair.Quote,
                // TODO pitäisikö fee laskea itse? Fee = new PriceValue(order.Fill_fees, quoteAsset),
                OpenTime = Common.Utils.UnixTimeToDateTime(binanceOrder.Time),
                FilledVolume = new PriceValue(binanceOrder.ExecutedQty, assetPair.Base),
                Volume = new PriceValue(binanceOrder.OrigQty, assetPair.Base),
                LimitPrice = new PriceValue(binanceOrder.StopPrice, assetPair.Quote),
                Side = side,
                Type = ParseOrderType(binanceOrder.Type),
                State = ParseState(binanceOrder.Status),
                CostExcludingFee = new PriceValue(binanceOrder.Price, assetPair.Quote) //TODO onko tämä oikea arvo?                
            };
        }

        private OrderState ParseState(string state)
        {
            switch (state.ToUpper())
            {
                case "NEW": return OrderState.Open;
                case "PARTIALLY_FILLED": return OrderState.Open; //TODO should we have more OrderStates to handle these options?
                case "FILLED": return OrderState.Closed;
                case "CANCELED": return OrderState.Cancelled;
                // case "PENDING_CANCEL": return OrderState.Cancelled; currenty unused
                case "REJECTED": return OrderState.Cancelled;
                case "EXPIRED": return OrderState.Cancelled;
                default:
                    m_logger.Error("BinanceExchange.ParseState: unknown value '{0}", state);
                    return OrderState.Unknown;
            }
        }

        private OrderSide ParseSide(string side)
        {
            switch (side.ToUpper())
            {
                case "BUY": return OrderSide.Buy;
                case "SELL": return OrderSide.Sell;
                default:
                    m_logger.Error("BinanceExchange.ParseSide: unknown value '{0}", side);
                    return OrderSide.Unknown;
            }
        }

        private Binance.API.Csharp.Client.Models.Enums.OrderSide ParseSide(OrderSide ownSide)
        {
            switch (ownSide)
            {
                case OrderSide.Buy : return Binance.API.Csharp.Client.Models.Enums.OrderSide.BUY;
                case OrderSide.Sell: return Binance.API.Csharp.Client.Models.Enums.OrderSide.SELL;
                default:
                    throw new InvalidOperationException(string.Format("BinanceExchange.ParseSide: unknown value '{0}", ownSide));
            }
        }

        private OrderType ParseOrderType(string value)
        {
            switch (value.ToUpper())
            {
                case "LIMIT": return OrderType.Limit;
                case "MARKET": return OrderType.Market;
                case "STOP_LOSS": return OrderType.Other;
                case "STOP_LOSS_LIMIT": return OrderType.Other;
                case "TAKE_PROFIT": return OrderType.Other;
                case "TAKE_PROFIT_LIMIT": return OrderType.Other;
                case "LIMIT_MAKER": return OrderType.Other;
                default:
                    m_logger.Error("BinanceExchange.ParseOrderType: unknown value '{0}'", value);
                    return OrderType.Unknown;
            }
        }
        
        private MinimalOrder ParseNewOrder(NewOrder newOrder, OrderSide side)
        {
            if (newOrder != null)
            {
                return new MinimalOrder()
                {
                    Id = new OrderId(newOrder.ClientOrderId),
                    Side = side
                };
            }
            else
            {
                return null;
            }
        }

        private CancelOrderResult ParseCancelOrderResult(CanceledOrder binanceOrder, OrderId idToMatch)
        {
            // atm. no check
            if (binanceOrder != null)
            {
                bool isOk = binanceOrder.OrigClientOrderId == idToMatch;
                return new CancelOrderResult()
                {
                    WasCancelled = isOk,
                    Error = isOk ? null : string.Format("binanceOrder.OrigClientOrderId {0} differs from idToMatch {1}", binanceOrder.OrigClientOrderId, idToMatch)
                };
            }
            else
            {
                return null;
            }
        }

        public Task<FullOrder> GetOrderInfo(OrderId id)
        {
            throw new NotImplementedException();
        }

        public async Task<FullOrder> GetOrderInfo2(OrderId id, AssetPair assetPair)
        {
            // TODO testaa putsaako Binance jossain tilanteessa ordereita vastaavasti kuin Gdax -> tarvittaessa vastaava cachetus 
            var binanceOrder = await m_client.GetOrder(assetPair.ShortName, origClientOrderId: id.Id);
            return ParseOrder(binanceOrder, assetPair);
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            return Task.FromResult(new PaymentMethodResult());
        }

        public async Task<dynamic> PlaceOrderTest(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume, OrderSide side)
        {
            //m_client.PostNewOrder(assetPair.ShortName, )
            API.Csharp.Client.Models.Enums.OrderSide binanceSide = API.Csharp.Client.Models.Enums.OrderSide.BUY;
            switch (side)
            {
                case OrderSide.Buy:
                    binanceSide = API.Csharp.Client.Models.Enums.OrderSide.BUY;
                    break;
                case OrderSide.Sell:
                    binanceSide = API.Csharp.Client.Models.Enums.OrderSide.SELL;
                    break;
                default:
                    throw new NotSupportedException(string.Format("BinanceExchange.PlaceOrderTest does not support side-value of {0}", side));
            }
            
            var result = await m_client.PostNewOrderTest(assetPair.ShortName, maxVolume.Value, limitPricePerUnit.Value, binanceSide);
            return result;
        }


        public async Task<MinimalOrder> PlaceImmediateBuyOrder(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume)
        {
            var orderSide = OrderSide.Buy;
            var newOrder = await PlaceImmediateBuyOrder2(assetPair, limitPricePerUnit, maxVolume, orderSide);
            var result = ParseNewOrder(newOrder, orderSide);
            return result;
        }
        
        public async Task<NewOrder> PlaceImmediateBuyOrder2(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume, OrderSide s)
        {
            var orderSide = ParseSide(s);
            var result = await m_client.PostNewOrder(assetPair.ShortName, maxVolume.Value, limitPricePerUnit.Value, orderSide);
            return result;
        }

        public async Task<MinimalOrder> PlaceImmediateSellOrder(AssetPair assetPair, PriceValue minLimitPrice, PriceValue volume)
        {
            var orderSide = OrderSide.Sell;
            var newOrder = await PlaceImmediateBuyOrder2(assetPair, minLimitPrice, volume, orderSide);
            var result = ParseNewOrder(newOrder, orderSide);
            return result;
        }

        public Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            throw new NotImplementedException();
        }

        public async Task<CancelOrderResult> CancelOrder2(AssetPair assetPair, OrderId id)
        {
            var canceledOrder = await m_client.CancelOrder(assetPair.ShortName, origClientOrderId: id.Id);
            var result = ParseCancelOrderResult(canceledOrder, id);
            return result;
        }

        public Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address)
        {
            throw new NotImplementedException();
        }

        public Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account)
        {
            throw new NotImplementedException();
        }
    }
}
