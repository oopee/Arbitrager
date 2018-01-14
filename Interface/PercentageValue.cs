using System;
using System.Globalization;

namespace Interface
{
    public struct PercentageValue : IComparable
    {
        public static readonly PercentageValue Zero = new PercentageValue(0, true);
        public static readonly PercentageValue Invalid = new PercentageValue(0, false);

        public static readonly CultureInfo DefaultCulture1 = CultureInfo.CreateSpecificCulture("FI");
        public static readonly CultureInfo DefaultCulture2 = CultureInfo.CreateSpecificCulture("EN");

        /// <summary>
        /// Actual percentage value, eg 27.5 (%).
        /// </summary>
        public decimal Percentage
        {
            get
            {
                return Ratio * 100.0m;
            }
        }

        /// <summary>
        /// Percentage value ratio, e.g. percentage value of 70% has a ratio of 0.7
        /// </summary>
        public decimal Ratio { get; set; }

        /// <summary>
        /// Multiplier that can be used when adding change of this percentage amount to a value.
        /// 20% returns a change multiplier of 1.20, and -20% returns a multiplier of 0.8.
        /// </summary>
        public double ChangeMultiplier
        {
            get
            {
                return (double)(Ratio + 1);
            }
        }

        public bool Sign
        {
            get
            {
                return Ratio >= 0;
            }
        }
        
        public bool IsValid { get; private set; }

        private PercentageValue(decimal ratio)
            : this(ratio, true)
        {
        }

        private PercentageValue(decimal ratio, bool valid)
            : this()
        {
            Ratio = ratio;
            IsValid = valid;
        }

        /// <summary>
        /// Creates a PercentageValue from percentage value. Value of 20 means 20%, 0.5 means 0.5%.
        /// </summary>
        /// <param name="valueString">
        /// Percentage value, such as 20% or 57.5%
        /// </param>
        /// <returns>
        /// The <see cref="PercentageValue"/>.
        /// </returns>
        public static PercentageValue FromPercentage(decimal percentageValue)
        {
            return new PercentageValue(percentageValue / 100.0m);
        }

        /// <summary>
        /// Creates a PercentageValue from a ratio. Ratio of 0.5 means 50%.
        /// </summary>
        /// <param name="ratio">
        /// </param>
        /// <returns>
        /// The <see cref="PercentageValue"/>.
        /// </returns>
        public static PercentageValue FromRatio(decimal ratio)
        {
            return new PercentageValue(ratio);
        }

        /// <summary>
        /// Creates a PercentageValue from percentage string.
        /// </summary>
        /// <param name="valueString">
        /// Percentage string, such as 20% or 57.5%
        /// </param>
        /// <returns>
        /// The <see cref="PercentageValue"/>.
        /// </returns>
        public static PercentageValue FromString(string valueString)
        {
            if (valueString == null)
            {
                return Invalid;
            }

            valueString = valueString.TrimEnd(new[] { '%', ' ' });

            decimal outValue;

            if (decimal.TryParse(valueString, NumberStyles.Number, CultureInfo.CurrentCulture, out outValue))
            {
                return FromPercentage(outValue);
            }

            if (decimal.TryParse(valueString, NumberStyles.Number, DefaultCulture1, out outValue))
            {
                return FromPercentage(outValue);
            }

            if (decimal.TryParse(valueString, NumberStyles.Number, DefaultCulture2, out outValue))
            {
                return FromPercentage(outValue);
            }

            return Invalid;
        }

        public static PercentageValue operator *(PercentageValue value, int multiplier)
        {
            return new PercentageValue(value.Ratio * multiplier);
        }

        public static PercentageValue operator /(PercentageValue value, int divider)
        {
            return new PercentageValue(value.Ratio / divider);
        }

        public static PercentageValue operator *(int multiplier, PercentageValue value)
        {
            return value * multiplier;
        }

        public static PercentageValue operator *(decimal multiplier, PercentageValue value)
        {
            return new PercentageValue(value.Ratio * multiplier);
        }

        public static PercentageValue operator *(PercentageValue value, decimal multiplier)
        {
            return multiplier * value;
        }

        public static bool operator ==(PercentageValue a, PercentageValue b)
        {
            return a.IsValid == b.IsValid && a.Ratio == b.Ratio;
        }

        public static bool operator !=(PercentageValue a, PercentageValue b)
        {
            return !(a == b);
        }

        public static bool operator <(PercentageValue a, PercentageValue b)
        {
            return a.Ratio < b.Ratio;
        }

        public static bool operator >(PercentageValue a, PercentageValue b)
        {
            return a.Ratio > b.Ratio;
        }

        public static bool operator <=(PercentageValue a, PercentageValue b)
        {
            return a.Ratio <= b.Ratio;
        }

        public static bool operator >=(PercentageValue a, PercentageValue b)
        {
            return a.Ratio >= b.Ratio;
        }

        public static PercentageValue operator -(PercentageValue a)
        {
            return new PercentageValue(-a.Ratio);
        }

        public override bool Equals(object obj)
        {
            return obj is PercentageValue && this == (PercentageValue)obj;
        }

        public override string ToString()
        {
            return Percentage.ToString("0.##") + "%";
        }

        public override int GetHashCode()
        {
            return Ratio.GetHashCode();
        }

        /* too dangerous (is value ratio or percentage?)
        public static implicit operator PercentageValue(decimal value)
        {
            return new PercentageValue(value);
        }
        */

        public int CompareTo(object obj)
        {
            PercentageValue other = (PercentageValue)obj;

            if (IsValid && !other.IsValid)
            {
                return -1;
            }

            if (!IsValid && other.IsValid)
            {
                return 1;
            }

            return Ratio.CompareTo(other.Ratio);
        }
    }
}