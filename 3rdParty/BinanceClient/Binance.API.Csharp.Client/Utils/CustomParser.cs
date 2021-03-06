﻿using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.WebSocket;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Binance.API.Csharp.Client.Utils
{
    /// <summary>
    /// Class to parse some specific entities.
    /// </summary>
    public class CustomParser
    {
        /// <summary>
        /// Gets the orderbook data and generates an OrderBook object.
        /// </summary>
        /// <param name="orderBookData">Dynamic containing the orderbook data.</param>
        /// <returns></returns>
        public OrderBook GetParsedOrderBook(dynamic orderBookData)
        {
            var result = new OrderBook
            {
                LastUpdateId = orderBookData.lastUpdateId.Value
            };

            var bids = new List<OrderBookOffer>();
            var asks = new List<OrderBookOffer>();

            foreach (JToken item in ((JArray)orderBookData.bids).ToArray())
            {
                bids.Add(new OrderBookOffer() { Price = Common.Utils.StringToDecimal(item[0].ToString()), Quantity = Common.Utils.StringToDecimal(item[1].ToString()) });
            }

            foreach (JToken item in ((JArray)orderBookData.asks).ToArray())
            {
                asks.Add(new OrderBookOffer() { Price = Common.Utils.StringToDecimal(item[0].ToString()), Quantity = Common.Utils.StringToDecimal(item[1].ToString()) });
            }

            result.Bids = bids;
            result.Asks = asks;

            return result;
        }

        /// <summary>
        /// Gets the candlestick data and generates an Candlestick object.
        /// </summary>
        /// <param name="candlestickData">Dynamic containing the candlestick data.</param>
        /// <returns></returns>
        public IEnumerable<Candlestick> GetParsedCandlestick(dynamic candlestickData)
        {
            var result = new List<Candlestick>();

            foreach (JToken item in ((JArray)candlestickData).ToArray())
            {
                result.Add(new Candlestick()
                {
                    OpenTime = long.Parse(item[0].ToString()),
                    Open = Common.Utils.StringToDecimal(item[1].ToString()),
                    High = Common.Utils.StringToDecimal(item[2].ToString()),
                    Low = Common.Utils.StringToDecimal(item[3].ToString()),
                    Close = Common.Utils.StringToDecimal(item[4].ToString()),
                    Volume = Common.Utils.StringToDecimal(item[5].ToString()),
                    CloseTime = long.Parse(item[6].ToString()),
                    QuoteAssetVolume = Common.Utils.StringToDecimal(item[7].ToString()),
                    NumberOfTrades = int.Parse(item[8].ToString()),
                    TakerBuyBaseAssetVolume = Common.Utils.StringToDecimal(item[9].ToString()),
                    TakerBuyQuoteAssetVolume = Common.Utils.StringToDecimal(item[10].ToString())
                });
            }

            return result;
        }

        public DepthMessage GetParsedDepthMessage(dynamic messageData)
        {
            var result = new DepthMessage
            {
                EventType = messageData.e,
                EventTime = messageData.E,
                Symbol = messageData.s,
                UpdateId = messageData.u
            };

            var bids = new List<OrderBookOffer>();
            var asks = new List<OrderBookOffer>();

            foreach (JToken item in ((JArray)messageData.b).ToArray())
            {
                bids.Add(new OrderBookOffer() { Price = Common.Utils.StringToDecimal(item[0].ToString()), Quantity = Common.Utils.StringToDecimal(item[1].ToString()) });
            }

            foreach (JToken item in ((JArray)messageData.a).ToArray())
            {
                asks.Add(new OrderBookOffer() { Price = Common.Utils.StringToDecimal(item[0].ToString()), Quantity = Common.Utils.StringToDecimal(item[1].ToString()) });
            }

            result.Bids = bids;
            result.Asks = asks;

            return result;
        }
    }
}
