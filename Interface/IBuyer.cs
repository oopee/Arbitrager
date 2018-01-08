﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IBuyer : IExchange
    {
        Task<IAskOrderBook> GetAsks();
        Task<MyOrder> PlaceImmediateBuyOrder(decimal limitPricePerUnit, decimal maxVolume);
    }

    public class BalanceResult
    {
        public Dictionary<string, decimal> All { get; set; }
        public decimal Eth { get; set; }
        public decimal Eur { get; set; }
    }

    public class MyOrder
    {
        public OrderId Id { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Volume { get; set; }
        public decimal TotalPrice => PricePerUnit * Volume;
        public OrderType Type { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}), {2}€ x {3} (= {4}€)", Type, Id, PricePerUnit, Volume, TotalPrice);
        }
    }

    public enum OrderType
    {
        Unknown = 0,
        Buy = 1,
        Sell = 2
    }

    public enum OrderType2
    {
        Unknown = 0,
        Limit = 1,
        Market = 2,
        Other = 999
    }

    public enum OrderState
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
        Cancelled = 3
    }

    public class FullMyOrder
    {
        public OrderId Id { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Volume { get; set; }
        public decimal TotalPrice => PricePerUnit * Volume;
        public OrderType Type { get; set; }
        public OrderState State { get; set; }
        public OrderType2 OrderType { get; set; }

        public DateTime OpenTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public decimal FilledVolume { get; set; }
        public decimal Fee { get; set; }
        public decimal Cost { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2}), {3}€ x {4} (= {5}€). Filled: {6}", State, Type, Id, PricePerUnit, Volume, TotalPrice, FilledVolume);
        }
    }

    public struct OrderId
    {
        public string Id { get; set; }

        public OrderId(string id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id;
        }

        public static bool operator==(OrderId a, OrderId b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            return a.Id == b.Id;
        }

        public static bool operator!=(OrderId a, OrderId b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(OrderId))
            {
                return false;
            }

            OrderId o = (OrderId)obj;
            return this == o;
        }

        public static implicit operator string(OrderId id)
        {
            return id.Id;
        }
    }

    public struct PaymentMethodId
    {
        public string Id { get; set; }

        public PaymentMethodId(string id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
