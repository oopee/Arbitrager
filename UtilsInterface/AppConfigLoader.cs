using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public class AppConfigLoader
    {
        public static AppConfigLoader Instance = new AppConfigLoader();
        System.Xml.Linq.XElement m_settings;

        public AppConfigLoader()
        {
            using (var stream = new System.IO.FileStream("App.config", System.IO.FileMode.Open))
            {
                var doc = System.Xml.Linq.XDocument.Load(stream);
                m_settings = doc
                    .Element("configuration")
                    .Element("appSettings");
            }
        }

        public string AppSettings(string settingName)
        {
            return m_settings
                .Descendants("add")
                .Where(x => x.Attribute("key")?.Value == settingName)
                .Select(x => x.Attribute("value")?.Value)
                .FirstOrDefault();
        }
    }
}
