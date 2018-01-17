using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Kraken
{
    public class KrakenExchange : Interface.IExchange
    {
        KrakenApi.Kraken m_client;
        ILogger m_logger;

        public string Name => "Kraken";
        public PercentageValue TakerFeePercentage => PercentageValue.FromPercentage(0.26m); // 0.26%
        public PercentageValue MakerFeePercentage => PercentageValue.FromPercentage(0.16m); // 0.16%

        public KrakenConfiguration Configuration { get; private set; }

        public bool CanGetClosedOrders => true;

        public KrakenExchange(KrakenConfiguration configuration, ILogger logger)
        {
            Configuration = configuration;

            m_logger = logger.WithName(GetType().Name);
            m_client = new KrakenApi.Kraken(
                logger.WithName("KrakenApi"),
                configuration.Key,
                configuration.Secret,
                3000);
        }

        public Task<MinimalOrder> PlaceImmediateBuyOrder(AssetPair assetPair, PriceValue limitPrice, PriceValue volume)
        {
            return PlaceImmediateOrder(assetPair, limitPrice, volume, OrderSide.Buy);
        }

        public Task<MinimalOrder> PlaceImmediateSellOrder(AssetPair assetPair, PriceValue limitPrice, PriceValue volume)
        {
            return PlaceImmediateOrder(assetPair, limitPrice, volume, OrderSide.Sell);
        }

        private async Task<MinimalOrder> PlaceImmediateOrder(AssetPair assetPair, PriceValue limitPrice, PriceValue volume, OrderSide side)
        {
            AssetPair.CheckPriceAndVolumeAssets(assetPair, limitPrice, volume);

            MinimalOrder myOrder = null;

            if (volume < 0)
            {
                throw new ArgumentException("volume must be non-negative");
            }

            var minimumOrderSize = GetMinimumOrderSizeForBaseCurrency(assetPair.Base);
            if (volume < minimumOrderSize)
            {
                throw new ArgumentException(string.Format("Volume must be >= {0}", minimumOrderSize));
            }

            string krakenSide;
            if (side == OrderSide.Buy)
            {
                krakenSide = "buy";
            }
            else if (side == OrderSide.Sell)
            {
                krakenSide = "sell";
            }
            else
            {
                throw new ArgumentException("side");
            }

            string krakenAssetPair = GetKrakenAssetPair(assetPair);

            await Task.Run(() =>
            {
                var order = new KrakenApi.KrakenOrder()
                { 
                    Pair = krakenAssetPair,
                    Type = krakenSide,
                    OrderType = "limit",
                    Price = limitPrice.Value,
                    Volume = volume.Value,
                    // ExpireTm = Common.Utils.DateTimeToUnixTime(TimeService.UtcNow.AddMinutes(1))
                };                

                var result = m_client.AddOrder(order);
                m_logger.Info("KrakenExchange: Placed {0} order for {1} -> {2} ({3})", krakenSide.ToUpper(), krakenAssetPair, result?.Descr.Order, result?.Descr.Close);

                myOrder = new MinimalOrder()
                {
                    Id = new OrderId(result.Txid.Single()), // TODO: what if there are multiple ids?
                    Side = OrderSide.Buy
                };

                // Kraken does not support Immediate or Cancel. Also, expiretm parameter does not work properly, so we have to cancel order manually.
                for (int i = 0; i < 5; ++i) // retry cancel 5 times
                {
                    try
                    {
                        m_client.CancelOrder(myOrder.Id.Id);
                        break;
                    }
                    catch (KrakenApi.KrakenException e)
                    {
                        if (e.Message == "EOrder:Unknown order")
                        {
                            break;
                        }
                    }
                    m_logger.Error("COULD NOT CANCEL KRAKEN ORDER! Retrying max 5 times..");
                }
            });

            return myOrder;
        }

        public async Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            OrderBook book = new OrderBook()
            {
                AssetPair = assetPair
            };

            await Task.Run(() =>
            {
                var krakenAssetPair = GetKrakenAssetPair(assetPair);
                Dictionary<string, KrakenApi.OrderBook> orderbookResult = m_client.GetOrderBook(krakenAssetPair, 15);
                var orderbook = orderbookResult[krakenAssetPair];

                var askOrders = orderbook.Asks
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = new PriceValue(x[0], assetPair.Quote),
                        VolumeUnits = new PriceValue(x[1], assetPair.Base),
                        Timestamp = Common.Utils.UnixTimeToDateTime((double)x[2])
                    })
                    .OrderBy(x => x.PricePerUnit);
                book.Asks.AddRange(askOrders);

                var bidOrders = orderbook.Bids
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = new PriceValue(x[0], assetPair.Quote),
                        VolumeUnits = new PriceValue(x[1], assetPair.Base),
                        Timestamp = Common.Utils.UnixTimeToDateTime((double)x[2])
                    })
                    .OrderBy(x => x.PricePerUnit);
                book.Bids.AddRange(bidOrders);
            });

            return book;
        }

        public async Task<BalanceResult> GetCurrentBalance(AssetPair assetPair)
        {
            var result = await Task.Run(() =>
            {
                var balance = m_client.GetAccountBalance();

                var krakenQuote = GetKrakenAsset(assetPair.Quote);
                var krakenBase = GetKrakenAsset(assetPair.Base);

                return new BalanceResult()
                {
                    AssetPair = assetPair,
                    All = balance,
                    QuoteCurrency = new PriceValue(balance.FirstOrDefault(x => x.Key == krakenQuote).Value, assetPair.Quote),
                    BaseCurrency = new PriceValue(balance.FirstOrDefault(x => x.Key == krakenBase).Value, assetPair.Base),
                };
            });

            return result;
        }
        
        /// <param name="account">This is the name of the account as specified in your Kraken withdrawal settings</param>
        public async Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account)
        {
            string refId = null;
            await Task.Run(() =>
            {
                refId = m_client.Withdraw(currency, account, amount);
            });

            return new WithdrawFiatResult()
            {
                ReferenceId = refId
            };
        }

        /// <param name="address">This is the name of the address as specified in your Kraken withdrawal settings</param>
        public async Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address)
        {
            string refId = null;
            await Task.Run(() =>
            {
                //var info = m_client.GetWithdrawInfo(currency, address, amount);
                //var status = m_client.GetWithdrawStatus(currency, info.Method);
                refId = m_client.Withdraw(currency, address, amount);
            });

            return new WithdrawCryptoResult()
            {
                ReferenceId = refId
            };
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            return Task.FromResult(new PaymentMethodResult());
        }

        public async Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            int count = 0;
            await Task.Run(() =>
            {
                var result = m_client.CancelOrder(id.Id);
                count = result.Count;
            });

            return new CancelOrderResult()
            {
                WasCancelled = count >= 1
            };
        }

        public async Task<List<FullOrder>> GetOpenOrders()
        {
            List<FullOrder> orders = new List<FullOrder>();

            await Task.Run(() =>
            {
                var openOrders = m_client.GetOpenOrders();

                foreach (var krakenOrder in openOrders)
                {
                    var order = ParseOrder(krakenOrder.Key, krakenOrder.Value);
                    orders.Add(order);
                }
            });

            return orders;
        }

        public async Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            List<FullOrder> orders = new List<FullOrder>();

            await Task.Run(() =>
            {
                int? start = Common.Utils.DateTimeToUnixTime(args?.StartUtc);
                int? end = Common.Utils.DateTimeToUnixTime(args?.EndUtc);

                var closedOrders = m_client.GetClosedOrders(start: start, end: end);
                foreach (var krakenOrder in closedOrders)
                {
                    var order = ParseOrder(krakenOrder.Key, krakenOrder.Value);
                    orders.Add(order);
                }
            });

            return orders;
        }

        public async Task<FullOrder> GetOrderInfo(OrderId id)
        {
            FullOrder order = null;

            await Task.Run(() =>
            {
                var result = m_client.QueryOrder(id);
                var r = result.Single();
                order = ParseOrder(r.Key, r.Value);
            });

            if (order?.Id != id)
            {
                throw new InvalidOperationException();
            }

            return order;
        }

        private FullOrder ParseOrder(string id, KrakenApi.OrderInfo value)
        {
            /*
           "open": {
               "OJF4I7-W22RM-GZ6ZJI": {
                   "refid": null,
                   "userref": null,
                   "status": "open",
                   "opentm": 1514669927.6341,
                   "starttm": 0,
                   "expiretm": 0,
                   "descr": {
                       "pair": "ETHEUR",
                       "type": "buy",
                       "ordertype": "limit",
                       "price": "1.00",
                       "price2": "0",
                       "leverage": "none",
                       "order": "buy 0.10000000 ETHEUR @ limit 1.00"
                   },
                   "vol": "0.10000000",
                   "vol_exec": "0.00000000",
                   "cost": "0.00000",
                   "fee": "0.00000",
                   "price": "0.00000",
                   "misc": "",
                   "oflags": "fciq"
               }
               */

            ParseProductId(value.Descr.Pair, out Asset baseAsset, out Asset quoteAsset);

            var order = new FullOrder()
            {
                Id = new OrderId(id),
                BaseAsset = baseAsset,
                QuoteAsset = quoteAsset,
                LimitPrice = null,
                Volume = new PriceValue(value.Volume, baseAsset),
                FilledVolume = new PriceValue(value.VolumeExecuted, baseAsset),
                Fee = new PriceValue(value.Fee, quoteAsset),
                CostExcludingFee = new PriceValue(value.Cost, quoteAsset),

                OpenTime = Common.Utils.UnixTimeToDateTime(value.OpenTm),
                ExpireTime = Common.Utils.UnixTimeToDateTimeNullable(value.ExpireTm),
                // StartTime = Common.Utils.UnixTimeToDateTimeNullable(value.StartTm),
                CloseTime = value.CloseTm != null ? Common.Utils.UnixTimeToDateTimeNullable(value.CloseTm.Value) : null,

                State = ParseState(value.Status),
                Side = ParseType(value.Descr.Type),
                Type = ParseOrderType(value.Descr.OrderType)
            };

            return order;
        }

        private void ParseProductId(string productId, out Asset baseAsset, out Asset quoteAsset)
        {
            string baseCurrency = productId.Substring(0, 3).ToUpper();
            string quoteCurrency = productId.Substring(3, 3).ToUpper();
            baseAsset = Asset.Get(baseCurrency);
            quoteAsset = Asset.Get(quoteCurrency);
        }

        private OrderState ParseState(string value)
        {
            switch (value)
            {
                case "open": return OrderState.Open;
                case "closed": return OrderState.Closed;
                case "canceled": return OrderState.Cancelled;
                default:
                    m_logger.Error("KrakernBuyer.ParseState: unknown value '{0}'", value);
                    return OrderState.Unknown;
            }
        }

        private OrderSide ParseType(string value)
        {
            switch (value)
            {
                case "buy": return OrderSide.Buy;
                case "sell": return OrderSide.Sell;
                default:
                    m_logger.Error("KrakernBuyer.ParseType: unknown value '{0}'", value);
                    return OrderSide.Unknown;
            }
        }

        private OrderType ParseOrderType(string value)
        {
            switch (value)
            {
                case "limit": return OrderType.Limit;
                case "market": return OrderType.Market;
                default:
                    m_logger.Error("KrakernBuyer.ParseOrderType: unknown value '{0}'", value);
                    return OrderType.Unknown;
            }
        }

        private static string GetKrakenAssetPair(AssetPair pair)
        {
            return string.Format("{0}{1}", GetKrakenAsset(pair.Base), GetKrakenAsset(pair.Quote));
        }

        private static string GetKrakenAsset(Asset asset)
        {
            var b = asset.ToString().ToUpper();

            if (b == "BTC")
            {
                b = "XBT";
            }

            string prefixB = (b == "EUR" || b == "USD" || b == "GBP" || b == "JPY" || b == "CAD") ? "Z" : "X";

            return string.Format("{0}{1}", prefixB, b);
        }

        // https://support.kraken.com/hc/en-us/articles/205893708-What-is-the-minimum-order-size-
        private static Dictionary<Asset, decimal> s_minimumOrderSizes = new Dictionary<Asset, decimal>()
        {
            // { Asset.REP, 0.3m }, // Augur
            { Asset.BTC, 0.002m }, // Bitcoin
            { Asset.BCH, 0.002m }, // Bitcoin Cash
            // { Asset.DASH, 0.03m }, // Dash
            // { Asset.DOGE, 3000m }, // Dogecoin
            // { Asset.EOS, 3m }, // EOS
            { Asset.ETH, 0.02m }, // Ethereum
            // { Asset.ETC, 0.3m }, // Ethereum Classic
            // { Asset.GNO, 0.03m }, // Gnosis
            // { Asset.ICN, 2m }, // Iconomi
            { Asset.LTC, 0.1m }, // Litecoin
            // { Asset.MLN, 0.1m }, // Melon
            // { Asset.XMR, 0.1m }, // Monero
            // { Asset.XRP, 30m }, // Ripple
            // { Asset.XLM, 300m }, // Stellar Lumens
            // { Asset.ZEC, 0.03m }, // Zcash
            { Asset.USDT, 5m }, // Tether
        };

        static PriceValue GetMinimumOrderSizeForBaseCurrency(Asset baseAsset)
        {
            if (s_minimumOrderSizes.TryGetValue(baseAsset, out decimal value))
            {
                return new PriceValue(value, baseAsset);
            }

            return new PriceValue(0m, baseAsset);
        }
    }
}
