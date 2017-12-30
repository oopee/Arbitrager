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

        public KrakenConfiguration Configuration { get; private set; }        

        public KrakenBuyer(KrakenConfiguration configuration)
        {
            Configuration = configuration;
            m_client = new KrakenClient.KrakenClient(
                configuration.Url,
                configuration.Version,
                configuration.Key,
                configuration.Secret);
        }

        public async Task PlaceBuyOrder()
        {
            await Task.Run(() =>
            {
                var order = new KrakenClient.KrakenOrder()
                {
                    Pair = "XETHZEUR",
                    Type = "buy",
                    OrderType = "limit",
                    Price = 1m,
                    Volume = 0.01m
                };

                var result = m_client.AddOrder(order);
                var r = GetResultAndThrowIfError(result);
            });
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
                    .Select(x => new Order()
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
                    .Select(x => new Order()
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

                // {{"error":[],"result":{"ZEUR":"2039.0751","XETH":"0.0000032900"}}}

                return new BalanceResult()
                {
                    All = all,
                    Eur = all.FirstOrDefault(x => x.Key == "ZEUR").Value,
                    Eth = all.FirstOrDefault(x => x.Key == "XETH").Value
                };
            });

            return result;
        }

        private JsonObject GetResultAndThrowIfError(Jayrock.Json.JsonObject obj)
        {
            if (obj == null)
            {
                throw new MyException("obj is null");
            }

            var error = obj["error"] as Jayrock.Json.JsonArray;
            if (error != null && error.Count > 0)
            {
                throw new MyException(string.Format("ERROR:\n\t", string.Join("\t", error.Select(x => x.ToString()))));
            }

            var result = (JsonObject)obj["result"];
            if (result == null)
            {
                throw new MyException(string.Format("result is null ({0})", obj));
            }

            return result;
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
