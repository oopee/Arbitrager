using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binance
{
    public class BinanceConfiguration
    {
        public string Key { get; set; }
        public string Secret { get; set; }
        public string Url => "https://api.binance.com";

        public static BinanceConfiguration FromAppConfig()
        {
            return new BinanceConfiguration()
            {
                Secret = Utils.AppConfigLoader.Instance.AppSettings("BinanceSecret") ?? "",
                Key = Utils.AppConfigLoader.Instance.AppSettings("BinanceKey") ?? "",
            };
        }
    }
}
