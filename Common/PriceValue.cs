using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public enum RoundingStrategy
    {
        Default,
        AlwaysRoundUp,
        AlwaysRoundDown,
    }

    public enum AssetType
    {
        EUR,
        ETH
    }

    public struct Asset
    {
        public static readonly Asset ETH = new Asset(AssetType.ETH, RoundingStrategy.AlwaysRoundDown, 4);
        public static readonly Asset EUR = new Asset(AssetType.EUR, RoundingStrategy.AlwaysRoundDown, 2);

        public AssetType Type { get; set; }
        public string Name => Enum.GetName(typeof(AssetType), Type);

        private RoundingStrategy m_roundingStrategy;
        private int m_decimalPlaces;

        public Asset(AssetType type, RoundingStrategy rounding, int decimalPlaces)
        {
            Type = type;
            m_roundingStrategy = rounding;
            m_decimalPlaces = decimalPlaces;
        }

        public decimal Round(decimal v, bool sign, RoundingStrategy? r = null, int? decimalPlaces = null)
        {
            // todo negatiivinen numero?
            RoundingStrategy roundingStrategyToUse = r ?? m_roundingStrategy;
            int decimalPlacesToUse = decimalPlaces ?? m_decimalPlaces;

            switch (roundingStrategyToUse)
            {
                case RoundingStrategy.Default:
                    return Math.Round(v, decimalPlacesToUse);
                case RoundingStrategy.AlwaysRoundDown:
                    return sign ? RoundDown(v, decimalPlacesToUse) : RoundUp(v, decimalPlacesToUse);
                case RoundingStrategy.AlwaysRoundUp:
                    return sign ? RoundUp(v, decimalPlacesToUse) : RoundDown(v, decimalPlacesToUse);
                default:
                    throw new NotImplementedException(string.Format("Unknown RoundingAccuracy {0}", Enum.GetName(typeof(RoundingStrategy), roundingStrategyToUse)));
            }
        }

        private decimal RoundDown(decimal i, double decimalPlaces)
        {
            var power = Convert.ToDecimal(Math.Pow(10, decimalPlaces));
            return Math.Floor(i * power) / power;
        }

        private decimal RoundUp(decimal i, double decimalPlaces)
        {
            var power = Convert.ToDecimal(Math.Pow(10, decimalPlaces));
            return Math.Ceiling(i * power) / power;
        }

        public static bool operator ==(Asset a, Asset b)
        {
            return a.Name == b.Name;
        }

        public static bool operator !=(Asset a, Asset b)
        {
            return a.Name != b.Name;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct PriceValue : IComparable
    {
        public decimal Value { get; private set; }
        public Asset Asset { get; private set; }
        public bool Sign => Value >= 0;

        public PriceValue(decimal value, Asset asset)
        {
            Value = value;
            Asset = asset;
        }

        public static PriceValue FromETH(decimal value)
        {
            return new PriceValue(value, Asset.ETH);
        }

        public static PriceValue FromEUR(decimal value)
        {
            return new PriceValue(value, Asset.EUR);
        }

        /// <summary>
        /// Round this PriceValue according given parameters or with default method defined to the asset
        /// </summary>
        /// <param name="strategy">If left empty, uses default strategy defined for current asset</param>
        /// <param name="decimalPlaces">If left empty, uses default decimalPlaces value defined for current asset</param>
        /// <returns></returns>
        public PriceValue Round(RoundingStrategy? strategy = null, int? decimalPlaces = null)
        {
            var roundedValue = Asset.Round(Value, Sign, strategy, decimalPlaces);
            return new PriceValue(roundedValue, Asset);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Value, Asset);
        }

        public int CompareTo(object obj)
        {
            // Todo take account asset

            PriceValue other = (PriceValue)obj;
            if (Asset != other.Asset)
            {
                return -1;
            }
            return (int)(Value - other.Value);
        }

        public static PriceValue operator +(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot sum different assets");
            return new PriceValue(a.Value + b.Value, a.Asset);
        }

        public static PriceValue operator -(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot subtract different assets");
            return new PriceValue(a.Value - b.Value, a.Asset);
        }

        public static PriceValue operator *(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot multiply different assets");
            return new PriceValue(a.Value * b.Value, a.Asset);
        }

        public static PriceValue operator *(PriceValue p, decimal multiplier)
        {
            return new PriceValue(multiplier * p.Value, p.Asset);
        }

        public static PriceValue operator *(PriceValue p, int multiplier)
        {
            return new PriceValue(multiplier * p.Value, p.Asset);
        }

        public static PriceValue operator /(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot divide different assets");
            return new PriceValue(a.Value / b.Value, a.Asset);
        }

        public static bool operator ==(PriceValue a, PriceValue b)
        {
            return a.Asset == b.Asset && a.Value == b.Value;
        }

        public static bool operator !=(PriceValue a, PriceValue b)
        {
            return a.Asset != b.Asset || a.Value == b.Value;
        }

        public static bool operator ==(PriceValue b, decimal a)
        {
            return a == b.Value;
        }

        public static bool operator !=(PriceValue b, decimal a)
        {
            return a == b.Value;
        }

        public static bool operator <(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare value greatness for different assets");
            return a.Value < b.Value;
        }

        public static bool operator >(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare value greatness for different assets");
            return a.Value > b.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is PriceValue && this == (PriceValue)obj;
        }

        public override int GetHashCode()
        {
            return (Value + (decimal)Asset.Type).GetHashCode();
        }
    }
}
