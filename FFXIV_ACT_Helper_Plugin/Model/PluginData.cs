using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FFXIV_ACT_Helper_Plugin
{
    [XmlRoot("data")]
    public class PluginData
    {
        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("url")]
        public string Url { get; set; }
    }
}
