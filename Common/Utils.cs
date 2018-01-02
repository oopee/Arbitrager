using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Utils
    {
        public static decimal StringToDecimal(string str)
        {
            return decimal.Parse(str, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static DateTime? UnixTimeToDateTimeNullable(long unixTime)
        {
            if (unixTime <= 0)
            {
                return null;
            }

            return UnixTimeToDateTime(unixTime);
        }

        public static DateTime? UnixTimeToDateTimeNullable(double unixTime)
        {
            if (unixTime <= 0)
            {
                return null;
            }

            return UnixTimeToDateTime(unixTime);
        }

        public static DateTime UnixTimeToDateTime(long unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
        }

        public static DateTime UnixTimeToDateTime(double unixTime)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)(unixTime * 1000)).UtcDateTime;
        }

        public static string DateTimeToUnixTimeString(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return null;
            }

            if (dateTime.Value.Kind != DateTimeKind.Utc)
            {
                throw new InvalidOperationException("DateTime must be UTC");
            }

            var offset = new DateTimeOffset(dateTime.Value);

            var millis = offset.ToUnixTimeMilliseconds();
            var seconds = millis / 1000;
            var fraction = millis % 1000;
            if (fraction > 0)
            {
                return string.Format("{0}.{1}", seconds, millis);
            }
            else
            {
                return seconds.ToString();
            }
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}