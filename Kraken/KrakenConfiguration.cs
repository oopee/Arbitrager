using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Kraken
{
    public class KrakenConfiguration
    {
        public string Key { get; set; }
        public string Secret { get; set; }

        public string Url => "https://api.kraken.com";
        public int Version => 0;

        public static KrakenConfiguration FromAppConfig()
        {
            return new KrakenConfiguration()
            {
                Secret = Utils.AppConfigLoader.Instance.AppSettings("KrakenSecret") ?? "",
                Key = Utils.AppConfigLoader.Instance.AppSettings("KrakenKey") ?? "",
            };
        }
    }
}
