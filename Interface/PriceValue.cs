using System;
using System.Collections.Generic;
using System.Text;

namespace Interface
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
        ETH,
        BTC,
        LTC,
        BCH,
        USDT,
        USD,
        NEO
    }

    public class AssetSettings
    {
        public static Dictionary<AssetType, AssetSettings> DefaultSettings = new Dictionary<AssetType, AssetSettings>()
        {
            { AssetType.EUR, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 2 } },
            { AssetType.USD, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 2 } },
            { AssetType.ETH, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 18  } },
            { AssetType.BTC, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 8  } },
            { AssetType.LTC, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 8  } },
            { AssetType.BCH, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 8  } },
            { AssetType.USDT, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 2  } },
            { AssetType.NEO, new AssetSettings() { DefaultRoundingStrategy = RoundingStrategy.AlwaysRoundDown, DecimalPlaces = 0  } },
        };

        public RoundingStrategy DefaultRoundingStrategy { get; set; }
        public int DecimalPlaces { get; set; }
    }

    public struct Asset
    {
        // 1 ETH = 1000000000000000000 wei (normalized: 100000000000000000000)
        public static readonly Asset ETH = new Asset(AssetType.ETH);

        // 1 EUR = 100 cents  (normalized: 10000)
        public static readonly Asset EUR = new Asset(AssetType.EUR);

        public static readonly Asset USD = new Asset(AssetType.USD);
        public static readonly Asset BTC = new Asset(AssetType.BTC);
        public static readonly Asset LTC = new Asset(AssetType.LTC);
        public static readonly Asset BCH = new Asset(AssetType.BCH);
        public static readonly Asset USDT = new Asset(AssetType.USDT);
        public static readonly Asset NEO = new Asset(AssetType.NEO);

        public AssetType Type { get; set; }
        public string Name { get; set; }
        public RoundingStrategy RoundingStrategy { get; private set; }
        public int DecimalPlaces { get; private set; }

        public static Asset Get(string name)
        {
            var val = (AssetType)Enum.Parse(typeof(AssetType), name);
            if (Enum.IsDefined(typeof(AssetType), val))
            {
                return new Asset(val);
            }

            throw new NotSupportedException(string.Format("Invalid currency/asset: {0}", name));
        }

        public Asset(AssetType type, RoundingStrategy? rounding = null, int? decimalPlaces = null)
        {
            Name = type.ToString().ToUpper();
            Type = type;
            RoundingStrategy = rounding ?? AssetSettings.DefaultSettings[type].DefaultRoundingStrategy;
            DecimalPlaces = decimalPlaces ?? AssetSettings.DefaultSettings[type].DecimalPlaces;
        }
        
        public decimal Round(decimal v, RoundingStrategy? r = null, int? decimalPlaces = null)
        {
            var sing = v >= 0;
            RoundingStrategy roundingStrategyToUse = r ?? RoundingStrategy;
            int decimalPlacesToUse = decimalPlaces ?? DecimalPlaces;

            switch (roundingStrategyToUse)
            {
                case RoundingStrategy.Default:
                    return Math.Round(v, decimalPlacesToUse);
                case RoundingStrategy.AlwaysRoundDown:
                    return sing ? RoundDown(v, decimalPlacesToUse) : RoundUp(v, decimalPlacesToUse);
                case RoundingStrategy.AlwaysRoundUp:
                    return sing ? RoundUp(v, decimalPlacesToUse) : RoundDown(v, decimalPlacesToUse);
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

    /// <summary>
    /// A currency/asset pair is the quotation and pricing structure of the currencies traded in the forex market; 
    /// the value of a currency is a rate and is determined by its comparison to another currency. 
    /// The first listed currency of a currency pair is called the base currency, and the second currency is 
    /// called the quote currency. The currency pair indicates how much of the quote currency is needed to 
    /// purchase one unit of the base currency.
    /// </summary>
    public class AssetPair
    {
        public static readonly AssetPair EthEur = new AssetPair(Asset.ETH, Asset.EUR);
        public static readonly AssetPair NeoUsdt = new AssetPair(Asset.NEO, Asset.USDT);

        /// <summary>
        /// Base asset/currency. The base currency represents how much of the quote currency is needed for you to get one unit of the base currency.
        /// </summary>
        public Asset Base { get; private set; }

        /// <summary>
        /// Quote asset/currency. See base currency.
        /// </summary>
        public Asset Quote { get; private set; }

        public string ShortName => string.Format("{0}{1}", Base, Quote);
        public string DisplayName => string.Format("{0}/{1}", Base, Quote);

        public AssetPair(Asset baseAsset, Asset quoteAsset)
        {
            Base = baseAsset;
            Quote = quoteAsset;
        }

        private AssetPair()
        {

        }

        public override int GetHashCode()
        {
            return Base.GetHashCode() * 13 + Quote.GetHashCode();
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(AssetPair))
            {
                return false;
            }

            var o = (AssetPair)obj;
            return o.Base == Base && o.Quote == Quote;
        }

        public static bool operator==(AssetPair a, AssetPair b)
        {
            return object.Equals(a, b);
        }

        public static bool operator!=(AssetPair a, AssetPair b)
        {
            return !(a == b);
        }

        public static void CheckPriceAndVolumeAssets(Interface.AssetPair assetPair, Interface.PriceValue price, Interface.PriceValue volume)
        {
            if (volume.Asset != assetPair.Base)
            {
                throw new ArgumentException(string.Format("Volume must be in base currency. Current pair: {0}, Volume: {1}", assetPair, volume.Asset));
            }

            if (price.Asset != assetPair.Quote)
            {
                throw new ArgumentException(string.Format("Price must be in quote currency. Current pair: {0}, Price: {1}", assetPair, price.Asset));
            }
        }
    }

    public struct PriceValue : IComparable
    {
        public decimal Value { get; private set; }
        public Asset Asset { get; private set; }
        public bool Sign => Value >= 0;
        public bool IsValid { get; private set; }

        public PriceValue(decimal value, Asset asset)
        {
            Value = value;
            Asset = asset;
            IsValid = true;
        }

        public static PriceValue FromNEO(decimal value)
        {
            return new PriceValue(value, Asset.NEO);
        }

        public static PriceValue FromETH(decimal value)
        {
            return new PriceValue(value, Asset.ETH);
        }

        public static PriceValue FromEUR(decimal value)
        {
            return new PriceValue(value, Asset.EUR);
        }

        public static PriceValue FromUSDT(decimal value)
        {
            return new PriceValue(value, Asset.USDT);
        }

        public static PriceValue? FromETH(decimal? value)
        {
            return value.HasValue ? (PriceValue?)new PriceValue(value.Value, Asset.ETH) : null;
        }
        
        public static PriceValue? FromEUR(decimal? value)
        {
            return value.HasValue ? (PriceValue?)new PriceValue(value.Value, Asset.EUR) : null;
        }

        /// <summary>
        /// Round this PriceValue according given parameters or with default method defined to the asset
        /// </summary>
        /// <param name="strategy">If left empty, uses default strategy defined for current asset</param>
        /// <param name="decimalPlaces">If left empty, uses default decimalPlaces value defined for current asset</param>
        /// <returns></returns>
        public PriceValue Round(RoundingStrategy? strategy = null, int? decimalPlaces = null)
        {
            var roundedValue = Asset.Round(Value, strategy, decimalPlaces);
            return new PriceValue(roundedValue, Asset);
        }
        
        public PriceValue AddPercentage(PercentageValue percentage)
        {
            return this * (decimal)percentage.ChangeMultiplier;
        }

        public PriceValue SubtractPercentage(PercentageValue percentage)
        {
            return this * (1.0m - percentage.Ratio);
        }
        
        public override string ToString()
        {
            return Value.ToString("N" + Asset.DecimalPlaces);
        }

        public string ToStringWithAsset()
        {
            return string.Format("{0} {1}", this, Asset);
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

        public PriceValue AsZero()
        {
            return new PriceValue(0m, Asset);
        }

        public static PriceValue InvalidValue => new PriceValue() { IsValid = false };

        public static PriceValue operator *(PriceValue price, PercentageValue percentage)
        {
            return price * percentage.Ratio;
        }

        public static PriceValue operator +(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot sum different assets");
            return new PriceValue(a.Value + b.Value, a.Asset);
        }

        public static PriceValue operator +(PriceValue a, decimal b)
        {
            return new PriceValue(a.Value + b, a.Asset);
        }

        public static PriceValue operator -(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot subtract different assets");
            return new PriceValue(a.Value - b.Value, a.Asset);
        }

        public static PriceValue operator -(PriceValue a, decimal b)
        {
            return new PriceValue(a.Value - b, a.Asset);
        }

        public static PriceValue operator -(PriceValue a)
        {
            return new PriceValue(-a.Value, a.Asset);
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
            return b.Value != 0m ? new PriceValue(a.Value / b.Value, a.Asset) : new PriceValue(0m, a.Asset);
        }

        public static PriceValue operator /(PriceValue a, decimal b)
        {
            return b != 0m ? new PriceValue(a.Value / b, a.Asset) : new PriceValue(0m, a.Asset);
        }        

        public static bool operator ==(PriceValue a, PriceValue b)
        {
            return a.Asset == b.Asset && a.Value == b.Value;
        }

        public static bool operator !=(PriceValue a, PriceValue b)
        {
            return a.Asset != b.Asset || a.Value != b.Value;
        }

        public static bool operator ==(PriceValue b, decimal a)
        {
            return a == b.Value;
        }

        public static bool operator !=(PriceValue b, decimal a)
        {
            return a != b.Value;
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

        public static bool operator <=(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare value greatness for different assets");
            return a.Value <= b.Value;
        }

        public static bool operator >=(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare value greatness for different assets");
            return a.Value >= b.Value;
        }

        public static bool operator <(PriceValue a, decimal b)
        {
            return a.Value < b;
        }

        public static bool operator <=(PriceValue a, decimal b)
        {
            return a.Value <= b;
        }

        public static bool operator >=(PriceValue a, decimal b)
        {
            return a.Value >= b;
        }

        public static bool operator >(PriceValue a, decimal b)
        {
            return a.Value > b;
        }

        public static bool operator <(decimal a, PriceValue b)
        {
            return a < b.Value;
        }

        public static bool operator <=(decimal a, PriceValue b)
        {
            return a <= b.Value;
        }

        public static bool operator >=(decimal a, PriceValue b)
        {
            return a >= b.Value;
        }

        public static bool operator >(decimal a, PriceValue b)
        {
            return a > b.Value;
        }

        public static PriceValue Min(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare different assets");
            return new PriceValue(Math.Min(a.Value, b.Value), a.Asset);
        }

        public static PriceValue Max(PriceValue a, PriceValue b)
        {
            Guard.IsTrue(a.Asset == b.Asset, "cannot compare different assets");
            return new PriceValue(Math.Max(a.Value, b.Value), a.Asset);
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

    public static class PriceValueExtensions
    {
        public static PriceValue ToEUR(this decimal euroAmount)
        {
            return PriceValue.FromEUR(euroAmount);
        }

        public static PriceValue ToETH(this decimal ethAmount)
        {
            return PriceValue.FromETH(ethAmount);
        }
    }
}
