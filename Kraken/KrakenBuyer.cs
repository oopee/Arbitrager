using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Kraken
{
    public class KrakenBuyer : Interface.IBuyer
    {
        KrakenApi.Kraken m_client;
        ILogger m_logger;

        public string Name => "Kraken";
        public decimal TakerFeePercentage => 0.0026m; // 0.26%
        public decimal MakerFeePercentage => 0.0016m; // 0.16%

        public KrakenConfiguration Configuration { get; private set; }        

        public KrakenBuyer(KrakenConfiguration configuration, ILogger logger)
        {
            Configuration = configuration;

            m_logger = logger.WithName(GetType().Name);
            m_client = new KrakenApi.Kraken(
                logger.WithName("KrakenApi"),
                configuration.Key,
                configuration.Secret,
                3000);
        }

        public async Task<MinimalOrder> PlaceImmediateBuyOrder(decimal price, decimal volume)
        {
            MinimalOrder myOrder = null;

            await Task.Run(() =>
            {
                var order = new KrakenApi.KrakenOrder()
                { 
                    Pair = "XETHZEUR",
                    Type = "buy",
                    OrderType = "limit",
                    Price = price,
                    Volume = volume,
                    ExpireTmFromNow = 1 // expire after 1 second
                };                

                var result = m_client.AddOrder(order);
                m_logger.Info("KrakenBuyer: Placed order {0} ({1})", result?.Descr.Order, result?.Descr.Close);

                myOrder = new MinimalOrder()
                {
                    Id = new OrderId(result.Txid.Single()), // TODO: what if there are multiple ids?
                    Side = OrderSide.Buy
                };
            });

            return myOrder;
        }

        public async Task<IAskOrderBook> GetAsks()
        {
            var result = await GetOrderBook();
            return result;
        }

        private async Task<OrderBook> GetOrderBook()
        {
            OrderBook book = new OrderBook();

            await Task.Run(() =>
            {
                Dictionary<string, KrakenApi.OrderBook> orderbookResult = m_client.GetOrderBook("XETHZEUR", 15);
                var orderbook = orderbookResult["XETHZEUR"];

                var askOrders = orderbook.Asks
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = x[0],
                        VolumeUnits = x[1],
                        Timestamp = Common.Utils.UnixTimeToDateTime((double)x[2])
                    })
                    .OrderBy(x => x.PricePerUnit);
                book.Asks.AddRange(askOrders);

                var bidOrders = orderbook.Bids
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = x[0],
                        VolumeUnits = x[1],
                        Timestamp = Common.Utils.UnixTimeToDateTime((double)x[2])
                    })
                    .OrderBy(x => x.PricePerUnit);
                book.Bids.AddRange(bidOrders);
            });

            return book;
        }

        public async Task<BalanceResult> GetCurrentBalance()
        {
            var result = await Task.Run(() =>
            {
                var balance = m_client.GetAccountBalance();
               
                return new BalanceResult()
                {
                    All = balance,
                    Eur = balance.FirstOrDefault(x => x.Key == "ZEUR").Value,
                    Eth = balance.FirstOrDefault(x => x.Key == "XETH").Value
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

            var order = new FullOrder()
            {
                Id = new OrderId(id),
                LimitPrice = value.Price,
                Volume = value.Volume,
                FilledVolume = value.VolumeExecuted,
                Fee = value.Fee,
                CostExcludingFee = value.Cost,

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
                default:
                    m_logger.Error("KrakernBuyer.ParseOrderType: unknown value '{0}'", value);
                    return OrderType.Unknown;
            }
        }        
    }
}
