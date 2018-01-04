using System;
using System.Collections.Generic;
using System.Text;

namespace Gdax
{
    public class GdaxConfiguration
    {
        public string Key { get; set; }
        public string Signature { get; set; }
        public string Passphrase { get; set; }

        public static GdaxConfiguration FromAppConfig()
        {
            return new GdaxConfiguration()
            {
                Key = Utils.AppConfigLoader.Instance.AppSettings("GdaxKey") ?? "",
                Signature = Utils.AppConfigLoader.Instance.AppSettings("GdaxSecret") ?? "",
                Passphrase = Utils.AppConfigLoader.Instance.AppSettings("GdaxPassphrase") ?? "",
            };
        }
    }
}
