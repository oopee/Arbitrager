﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arbitrager.API.Models
{
    public class ArbitrageContext
    {
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }

        public string BuyerName { get; set; }
        public string SellerName { get; set; }

        public int State { get; set; }
        public string StateName { get; set; }
        
        public decimal UserDefinedQuoteCurrencyToSpend { get; set; }
        public decimal? BuyOrder_QuoteCurrencyLimitPriceToUse { get; set; }
        public decimal? BuyOrder_BaseCurrencyAmountToBuy { get; set; }

        public string Error { get; set; }
        public ArbitrageInfo Info { get; set; }

        public string BuyOrderId { get; set; }
        public decimal? BuyBaseCurrencyAmount { get; set; }
        public string SellOrderId { get; set; }

        public Order BuyOrder { get; set; }
        public Order SellOrder { get; set; }

        public FinishedResultData FinishedResult { get; set; }

        public static ArbitrageContext From(Interface.ArbitrageContext ctx)
        {
            return new ArbitrageContext()
            {
                BaseAsset = ctx.AssetPair.Base.Name,
                QuoteAsset = ctx.AssetPair.Quote.Name,
                BuyerName = ctx.Buyer?.Name,
                SellerName = ctx.Seller?.Name,
                State = (int)ctx.State,
                StateName = ctx.State.ToString(),
                UserDefinedQuoteCurrencyToSpend = ctx.UserDefinedQuoteCurrencyToSpend.Value,
                BuyOrder_QuoteCurrencyLimitPriceToUse = ctx.BuyOrder_QuoteCurrencyLimitPriceToUse?.Value,
                BuyOrder_BaseCurrencyAmountToBuy = ctx.BuyOrder_BaseCurrencyAmountToBuy?.Value,
                Error = ctx.Error?.ToString(),
                Info = ctx.Info != null ? ArbitrageInfo.From(ctx.Info) : null,
                BuyOrderId = ctx.BuyOrderId?.Id,
                BuyBaseCurrencyAmount = ctx.BuyBaseCurrencyAmount?.Value,
                SellOrderId = ctx.SellOrderId?.Id,
                BuyOrder = Order.From(ctx.BuyOrder),
                SellOrder = Order.From(ctx.SellOrder),
                FinishedResult = FinishedResultData.From(ctx.FinishedResult),
            };
        }
    }

    public class Order
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// BUY or SELL.
        /// </summary>
        public string Side { get; set; }

        /// <summary>
        /// OPEN, CLOSED or CANCELLED.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// MARKET or LIMIT.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The total volume of BASE currency to buy/sell.
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// The actual/executed value of BASE currency that was bought/sold. The final value of this property is determined after
        /// the order has been closed/cancelled.
        /// </summary>
        public decimal FilledVolume { get; set; }

        /// <summary>
        /// The limit price used in this order. This is applicable only if order type is 'Limit'.
        /// </summary>
        public decimal? LimitPrice { get; set; }

        /// <summary>
        /// A fee in QUOTE currency. The fee may be set only after order has been closed/cancelled. Note that for buy orders the fee increases the total cost
        /// (i.e. causes more quote currency to be spent) and for sell orders the fee decreases the total cost (i.e. causes less quote currency to be earned).
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// A cost of this order in QUOTE currency WITHOUT FEES.
        /// </summary>
        public decimal CostExcludingFee { get; set; }

        /// <summary>
        /// A cost of this order in QUOTE currency WITH FEES. Note that for buy orders this is greater or equal to CostExcludingFee 
        /// and for sell this is less or equal to CostExcludingFee.
        /// </summary>
        public decimal CostIncludingFee { get; set; }

        /// <summary>
        /// Timestamp when this order was created and opened.
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Timestamp when this order was closed/cancelled.
        /// </summary>
        public DateTime? CloseTime { get; set; }

        /// <summary>
        /// Timestamp when this order expires automatically.
        /// </summary>
        public DateTime? ExpireTime { get; set; }

        /// <summary>
        /// Average unit price, basically CostIncludingFee / FilledVolume.
        /// </summary>
        public decimal AverageUnitPrice { get; set; }

        public static Order From(Interface.FullOrder order)
        {
            if (order == null)
            {
                return null;
            }

            return new Order()
            {
                AverageUnitPrice = order.AverageUnitPrice.Value,
                CloseTime = order.CloseTime,
                CostExcludingFee = order.CostExcludingFee.Value,
                CostIncludingFee = order.CostIncludingFee.Value,
                ExpireTime = order.ExpireTime,
                Fee = order.Fee.Value,
                FilledVolume = order.FilledVolume.Value,
                Id = order.Id.Id,
                LimitPrice = order.LimitPrice?.Value,
                OpenTime = order.OpenTime,
                Side = order.Side.ToString(),
                State = order.State.ToString(),
                Type = order.Type.ToString(),
                Volume = order.Volume.Value,
            };
        }
    }

    public class FinishedResultData
    {
        public decimal BaseCurrencyBought { get; set; }
        public decimal BaseCurrencySold { get; set; }
        public decimal QuoteCurrencySpent { get; set; }
        public decimal QuoteCurrencyEarned { get; set; }

        public Balance BuyerBalance { get; set; }
        public Balance SellerBalance { get; set; }

        public decimal QuoteCurrencyDelta { get; set; }
        public decimal BaseCurrencyDelta { get; set; }
        public decimal ProfitPercentage { get; set; }

        public static FinishedResultData From(Interface.ArbitrageContext.FinishedResultData data)
        {
            if (data == null)
            {
                return null;
            }

            return new FinishedResultData()
            {
                BuyerBalance = Balance.From(data.BuyerBalance),
                SellerBalance = Balance.From(data.SellerBalance),
                BaseCurrencyBought = data.BaseCurrencyBought.Value,
                BaseCurrencySold = data.BaseCurrencySold.Value,
                QuoteCurrencySpent = data.QuoteCurrencySpent.Value,
                QuoteCurrencyEarned = data.QuoteCurrencyEarned.Value,
                BaseCurrencyDelta = data.BaseCurrencyDelta.Value,
                QuoteCurrencyDelta = data.QuoteCurrencyDelta.Value,
                ProfitPercentage = data.ProfitPercentage.Percentage,
            };
        }
    }
}
