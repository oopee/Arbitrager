using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

using GDAXClient;

namespace Gdax
{
    public class GdaxExchange : Interface.IExchange
    {
        GDAXClient.Authentication.Authenticator m_authenticator;
        GDAXClient.GDAXClient m_client;
        ILogger m_logger;

        Dictionary<string, FullOrder> s_orders = new Dictionary<string, FullOrder>();

        public string Name => "GDAX";
        public PercentageValue TakerFeePercentage => PercentageValue.FromPercentage(0.3m); // 0.3%
        public PercentageValue MakerFeePercentage => PercentageValue.FromPercentage(0.0m); // 0%

        public bool CanGetClosedOrders => false;

        public GdaxExchange(GdaxConfiguration configuration, ILogger logger, bool isSandbox)
        {
            m_logger = logger.WithName(GetType().Name);
            m_authenticator = new GDAXClient.Authentication.Authenticator(configuration.Key, configuration.Signature, configuration.Passphrase);
            m_client = new GDAXClient.GDAXClient(m_authenticator, sandBox: isSandbox);
        }

        public async Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            var gdaxProductType = GetGdaxProductTypeFromAssetPair(assetPair);

            var result = await m_client.ProductsService.GetProductOrderBookAsync(
                gdaxProductType, 
                GDAXClient.Products.ProductsService.OrderBookLevel.Top50);

            var orderBook = new OrderBook()
            {
                AssetPair = assetPair
            };

            if (result.Asks.Any())
            {
                orderBook.Asks.AddRange(result.Asks.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.First(), assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Skip(1).First(), assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            if (result.Bids.Any())
            {
                orderBook.Bids.AddRange(result.Bids.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.First(), assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Skip(1).First(), assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            return orderBook;
        }

        private static GDAXClient.Services.Orders.ProductType GetGdaxProductTypeFromAssetPair(AssetPair assetPair)
        {
            if (Enum.TryParse<GDAXClient.Services.Orders.ProductType>(assetPair.ShortName, true, out GDAXClient.Services.Orders.ProductType type))
            {
                return type;
            }

            throw new NotSupportedException(assetPair.ToString());
        }

        public async Task<BalanceResult> GetCurrentBalance(AssetPair assetPair)
        {
            var accounts = await m_client.AccountsService.GetAllAccountsAsync();

            var all = accounts.ToDictionary(x => x.Currency, x => x.Balance);

            var gdaxQuote = assetPair.Quote.ToString();
            var gdaxBase = assetPair.Base.ToString();

            return new BalanceResult()
            {
                AssetPair = assetPair,
                All = all,
                QuoteCurrency = new PriceValue(all.Where(x => x.Key == gdaxQuote).FirstOrDefault().Value, assetPair.Quote),
                BaseCurrency = new PriceValue(all.Where(x => x.Key == gdaxBase).FirstOrDefault().Value, assetPair.Base),
            };
        }

        public Task<MinimalOrder> PlaceImmediateBuyOrder(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume)
        {
            return PlaceImmediateOrder(assetPair, limitPricePerUnit, maxVolume, OrderSide.Buy);
        }

        public Task<MinimalOrder> PlaceImmediateSellOrder(AssetPair assetPair, PriceValue minLimitPrice, PriceValue volume)
        {
            return PlaceImmediateOrder(assetPair, minLimitPrice, volume, OrderSide.Sell);
        }

        private async Task<MinimalOrder> PlaceImmediateOrder(AssetPair assetPair, PriceValue limitPrice, PriceValue volume, OrderSide side)
        {
            AssetPair.CheckPriceAndVolumeAssets(assetPair, limitPrice, volume);

            GDAXClient.Services.Orders.OrderSide gdaxSide;
            if (side == OrderSide.Buy)
            {
                gdaxSide = GDAXClient.Services.Orders.OrderSide.Buy;
            }
            else if (side == OrderSide.Sell)
            {
                gdaxSide = GDAXClient.Services.Orders.OrderSide.Sell;
            }
            else
            {
                throw new ArgumentException("side");
            }

            var gdaxAssetPair = GetGdaxProductTypeFromAssetPair(assetPair);

            var order = await m_client.OrdersService.PlaceLimitOrderAsync(
                gdaxSide,
                gdaxAssetPair, 
                volume.Value, 
                limitPrice.Value, 
                GDAXClient.Services.Orders.TimeInForce.IOC);

            var orderResult = new MinimalOrder()
            {
                Id = new OrderId(order.Id.ToString()),
                Side = OrderSide.Sell
            };

            m_logger.Info("GdaxExchange: placed {0} order for {1} -> {2}", side.ToString().ToLower(), gdaxAssetPair, orderResult);

            // NOTE: GDAX may purge some (meaningless) orders. This means that if our order did not get filled (event partially),
            // it is possible that we cannot get the order from GDAX anymore. That is why we are storing all order results so that
            // GetOrderInfo() returns something meaningful for these orders.
            var fullOrder = ParseOrder(order);
            lock (s_orders)
            {
                // Purge all orders that are more than one day old
                var now = TimeService.UtcNow;
                var keysToRemove = s_orders.Where(x => x.Value.OpenTime + TimeSpan.FromDays(1) < now).Select(x => x.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    s_orders.Remove(key);
                }

                // Add order to cache
                s_orders[fullOrder.Id.Id] = fullOrder;
            }

            return orderResult;
        }

        public async Task<List<FullOrder>> GetOpenOrders()
        {
            var orders = await m_client.OrdersService.GetAllOrdersAsync();

            var result = orders.SelectMany(x => x).Select(x => ParseOrder(x)).ToList();
            return result;
        }

        public async Task<FullOrder> GetOrderInfo(OrderId id)
        {
            var order = await m_client.OrdersService.GetOrderByIdAsync(id.ToString());
            if (order == null)
            {
                // See comments in PlaceImmediateSellOrder.
                lock (s_orders)
                {
                    if (s_orders.TryGetValue(id.Id, out FullOrder fullOrder))
                    {
                        fullOrder.State = OrderState.Closed;
                        return fullOrder;
                    }
                }

                return null;
            }

            return ParseOrder(order);
        }

        public Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public async Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            var result = await m_client.OrdersService.CancelOrderByIdAsync(id.Id);

            return new CancelOrderResult()
            {
                WasCancelled = result.OrderIds.Select(x => x.ToString()).Contains(id.Id)
            };
        }

        /// <param name="account">Should be a payment_method_id</param>
        public async Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account)
        {
            var response = await m_client.WithdrawalsService.WithdrawFundsAsync(account, amount, CurrencyFromString(currency));

            return new WithdrawFiatResult()
            {
                ReferenceId = response.Id.ToString()
            };
        }

        public async Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address)
        {
            var response = await m_client.WithdrawalsService.WithdrawToCryptoAsync(address, amount, CurrencyFromString(currency));

            return new WithdrawCryptoResult()
            {
                ReferenceId = response.Id.ToString()
            };
        }

        public async Task<PaymentMethodResult> GetPaymentMethods()
        {
            var methods = await m_client.PaymentsService.GetAllPaymentMethodsAsync();

            return new PaymentMethodResult()
            {
                Methods = methods.Select(x => new PaymentMethod()
                {
                    Id = new PaymentMethodId(x.Id.ToString()),
                    Name = x.Name,
                    Currency = x.Currency,
                    RawResult = x
                }).ToList()
            };
        }

        private FullOrder ParseOrder(GDAXClient.Services.Orders.OrderResponse order)
        {
            var side = ParseSide(order.Side);

            ParseProductId(order.Product_id, out Asset baseAsset, out Asset quoteAsset);

            return new FullOrder()
            {
                Exchange = Name,

                Id = new OrderId(order.Id.ToString()),
                BaseAsset = baseAsset,
                QuoteAsset = quoteAsset,
                Fee = new PriceValue(order.Fill_fees, quoteAsset),
                OpenTime = order.Created_at,
                CloseTime = order.Done_at == default(DateTime) ? null : (DateTime?)order.Done_at,
                ExpireTime = null,
                FilledVolume = new PriceValue(order.Filled_size, baseAsset),
                Volume = new PriceValue(order.Size, baseAsset),
                LimitPrice = new PriceValue(order.Price, quoteAsset),
                Side = side,
                Type = ParseOrderType(order.Type),
                State = ParseState(order.Status),
                CostExcludingFee = new PriceValue(order.Executed_value, quoteAsset)
            };
        }

        private void ParseProductId(string productId, out Asset baseAsset, out Asset quoteAsset)
        {
            string baseCurrency = productId.Substring(0, 3).ToUpper();
            string quoteCurrency = productId.Substring(4, 3).ToUpper();
            baseAsset = Asset.Get(baseCurrency);
            quoteAsset = Asset.Get(quoteCurrency);
        }

        private OrderSide ParseSide(string side)
        {
            switch (side)
            {
                case "buy": return OrderSide.Buy;
                case "sell": return OrderSide.Sell;
                default:
                    m_logger.Error("GdaxSeller.ParseSide: unknown value '{0}", side);
                    return OrderSide.Unknown;
            }
        }

        private OrderType ParseOrderType(string type)
        {
            switch (type)
            {
                case "limit": return OrderType.Limit;
                default:
                    m_logger.Error("GdaxSeller.ParseOrderType: unknown value '{0}", type);
                    return OrderType.Unknown;
            }
        }

        private OrderState ParseState(string state)
        {
            switch (state)
            {
                case "open": return OrderState.Open;
                case "done": return OrderState.Closed;
                default:
                    m_logger.Error("GdaxSeller.ParseState: unknown value '{0}", state);
                    return OrderState.Unknown;
            }
        }

        private GDAXClient.Services.Currency CurrencyFromString(string currency)
        {
            if (currency == "ETH")
            {
                return GDAXClient.Services.Currency.ETH;
            }
            else if (currency == "EUR")
            {
                return GDAXClient.Services.Currency.EUR;
            }
            else
            {
                throw new NotImplementedException("Invalid currency code!");
            }
        }
    }
}
