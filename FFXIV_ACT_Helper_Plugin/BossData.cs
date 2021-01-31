using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FFXIV_ACT_Helper_Plugin
{
    [XmlRoot("data")]
    public class BossData
    {
        [XmlElement("timestamp")]
        public string Timestamp { get; set; }

        [XmlArray("bosses")]
        [XmlArrayItem("boss")]
        public List<Boss> Bosses { get; set; }

        public class Boss
        {
            [XmlElement("name")]
            public string Name { get; set; }

            [XmlElement("nameJa")]
            public string NameJa { get; set; }

            [XmlElement("zone")]
            public string Zone { get; set; }

            [XmlArray("percentiles")]
            [XmlArrayItem("percentile")]
            public List<Percentile> Percentiles { get; set; }

            [XmlArray("exclusionPeriods")]
            [XmlArrayItem("exclusionPeriod")]
            public List<ExclusionPeriod> ExclusionPeriods { get; set; }
        }

        public class Percentile
        {
            [XmlElement("job")]
            public string Job { get; set; }

            [XmlElement("perf1")]
            public int Perf1 { get; set; }

            [XmlElement("perf25")]
            public int Perf25 { get; set; }

            [XmlElement("perf50")]
            public int Perf50 { get; set; }

            [XmlElement("perf75")]
            public int Perf75 { get; set; }

            [XmlElement("perf95")]
            public int Perf95 { get; set; }

            [XmlElement("perf99")]
            public int Perf99 { get; set; }
        }

        public class ExclusionPeriod
        {
            [XmlElement("startTime")]
            public int StartTime { get; set; }

            [XmlElement("endTime")]
            public int EndTime { get; set; }
        }
    }
}
