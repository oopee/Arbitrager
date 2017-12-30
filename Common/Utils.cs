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

        public static DateTime UnixTimeToDateTime(long unixTime)
        {
            return epoch.AddSeconds(unixTime);
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}

namespace System
{
    public static class Extensions
    {
        public static StringBuilder AppendLine(this StringBuilder builder, string format, params object[] args)
        {
            if (args?.Length > 0)
            {
                format = string.Format(format, args);
            }

            return builder.AppendLine(format);
        }
    }
}
