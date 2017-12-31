using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;
using Jayrock.Json;

namespace Kraken
{
    public class KrakenBuyer : Interface.IBuyer
    {
        KrakenClient.KrakenClient m_client;
        ILogger m_logger;

        public string Name => "Kraken";
        public KrakenConfiguration Configuration { get; private set; }        

        public KrakenBuyer(KrakenConfiguration configuration, ILogger logger)
        {
            Configuration = configuration;

            m_logger = logger;
            m_client = new KrakenClient.KrakenClient(
                configuration.Url,
                configuration.Version,
                configuration.Key,
                configuration.Secret);
        }

        public async Task<MyOrder> PlaceBuyOrder(decimal price, decimal volume)
        {
            MyOrder myOrder = null;

            await Task.Run(() =>
            {
                var order = new KrakenClient.KrakenOrder()
                {
                    Pair = "XETHZEUR",
                    Type = "buy",
                    OrderType = "limit",
                    Price = price,
                    Volume = volume
                };                

                var result = m_client.AddOrder(order);
                var r = GetResultAndThrowIfError(result);

                m_logger.Info("KrakenBuyer: Placed order {0}", r);

                myOrder = new MyOrder()
                {
                    Ids = ((JsonArray)r["txid"]).Select(x => new OrderId((string)x)).ToList(),
                    PricePerUnit = price,
                    Volume = volume,
                    Type = OrderType.Buy
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
                var orderbookRaw = m_client.GetOrderBook("XETHZEUR", 15);
                var orderbookAll = GetResultAndThrowIfError(orderbookRaw);
                var orderbook = (JsonObject)orderbookAll["XETHZEUR"];

                var asks = orderbook["asks"] as JsonArray;
                if (asks != null)
                {
                    var askOrders = asks
                    .Cast<JsonArray>()
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = Common.Utils.StringToDecimal((string)x[0]),
                        VolumeUnits = Common.Utils.StringToDecimal((string)x[1]),
                        Timestamp = Common.Utils.UnixTimeToDateTime(((JsonNumber)x[2]).ToInt64())
                    })
                    .OrderBy(x => x.PricePerUnit);

                    book.Asks.AddRange(askOrders);
                }

                var bids = orderbook["bids"] as JsonArray;
                if (bids != null)
                {
                    var bidOrders = bids
                    .Cast<JsonArray>()
                    .Select(x => new OrderBookOrder()
                    {
                        PricePerUnit = Common.Utils.StringToDecimal((string)x[0]),
                        VolumeUnits = Common.Utils.StringToDecimal((string)x[1]),
                        Timestamp = Common.Utils.UnixTimeToDateTime(((JsonNumber)x[2]).ToInt64())
                    })
                    .OrderBy(x => x.PricePerUnit);

                    book.Bids.AddRange(bidOrders);
                }
            });

            return book;
        }

        public async Task<BalanceResult> GetCurrentBalance()
        {
            var result = await Task.Run(() =>
            {
                var balance = m_client.GetBalance();
                var r = GetResultAndThrowIfError(balance);

                Dictionary<string, decimal> all = new Dictionary<string, decimal>();
                foreach (var member in r)
                {
                    var val = decimal.Parse((string)member.Value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
                    all[member.Name] = val;
                }
                
                return new BalanceResult()
                {
                    All = all,
                    Eur = all.FirstOrDefault(x => x.Key == "ZEUR").Value,
                    Eth = all.FirstOrDefault(x => x.Key == "XETH").Value
                };
            });

            return result;
        }

        public async Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            await Task.Run(() =>
            {
                var result = m_client.CancelOrder(id.Id);
                var r = GetResultAndThrowIfError(result);
                int count = ((JsonNumber)r["count"]).ToInt32();

                if (count == 0)
                {
                    throw new MyException(string.Format("KrakenBuyer.CancelOrder: count is 0 ({0})", r));
                }
            });

            return new CancelOrderResult()
            {
                WasCancelled = true
            };
        }

        public async Task<List<FullMyOrder>> GetOpenOrders()
        {
            List<FullMyOrder> orders = new List<FullMyOrder>();
       
            await Task.Run(() =>
            {
                var result = m_client.GetOpenOrders();
                var r = GetResultAndThrowIfError(result);
                var open = (JsonObject)r["open"];
                foreach (var property in open)
                {
                    var order = ParseOrder(property.Name, (JsonObject)property.Value);
                    orders.Add(order);
                }
            });

            return orders;
        }

        public async Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            List<FullMyOrder> orders = new List<FullMyOrder>();

            await Task.Run(() =>
            {
                string start = Common.Utils.DateTimeToUnixTimeString(args?.StartUtc);
                string end = Common.Utils.DateTimeToUnixTimeString(args?.EndUtc);

                var result = m_client.GetClosedOrders(start: start ?? "", end: end ?? "");
                var r = GetResultAndThrowIfError(result);
                var closed = (JsonObject)r["closed"];
                foreach (var property in closed)
                {
                    var order = ParseOrder(property.Name, (JsonObject)property.Value);
                    orders.Add(order);
                }
            });

            return orders;
        }

        private FullMyOrder ParseOrder(string id, JsonObject value)
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

            var desc = (JsonObject)value["descr"];
            var order = new FullMyOrder()
            {
                Ids = new List<OrderId>() { new OrderId(id) },
                PricePerUnit = Common.Utils.StringToDecimal((string)value["price"]),
                Volume = Common.Utils.StringToDecimal((string)value["vol"]),
                FilledVolume = Common.Utils.StringToDecimal((string)value["vol_exec"]),
                Fee = Common.Utils.StringToDecimal((string)value["fee"]),
                Cost = Common.Utils.StringToDecimal((string)value["cost"]),

                OpenTime = Common.Utils.UnixTimeToDateTime(((JsonNumber)value["opentm"]).ToDouble()),
                ExpireTime = Common.Utils.UnixTimeToDateTimeNullable(((JsonNumber)value["expiretm"]).ToDouble()),
                StartTime = Common.Utils.UnixTimeToDateTimeNullable(((JsonNumber)value["starttm"]).ToDouble()),

                State = ParseState((string)value["status"]),
                Type = ParseType((string)desc["type"]),
                OrderType = ParseOrderType((string)desc["ordertype"])
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

        private OrderType ParseType(string value)
        {
            switch (value)
            {
                case "buy": return OrderType.Buy;
                case "sell": return OrderType.Sell;
                default:
                    m_logger.Error("KrakernBuyer.ParseType: unknown value '{0}'", value);
                    return OrderType.Unknown;
            }
        }

        private OrderType2 ParseOrderType(string value)
        {
            switch (value)
            {
                case "limit": return OrderType2.Limit;
                default:
                    m_logger.Error("KrakernBuyer.ParseOrderType: unknown value '{0}'", value);
                    return OrderType2.Unknown;
            }
        }

        private JsonObject GetResultAndThrowIfError(Jayrock.Json.JsonObject obj, [System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            try
            {
                if (obj == null)
                {
                    throw new MyException("obj is null");
                }

                var error = obj["error"] as Jayrock.Json.JsonArray;
                if (error != null && error.Count > 0)
                {
                    throw new MyException(string.Format("ERROR:\n\t{0}", string.Join("\t", error.Select(x => x.ToString()))));
                }

                var result = (JsonObject)obj["result"];
                if (result == null)
                {
                    throw new MyException(string.Format("result is null ({0})", obj));
                }

                return result;
            }
            catch (MyException e)
            {
                m_logger.Error("KrakenBuyer error in {0}: {1}", caller, e.Message);
                throw;
            }
        }
    }

    public class KrakenConfiguration
    {
        public string Url { get; set; }
        public int Version { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}
