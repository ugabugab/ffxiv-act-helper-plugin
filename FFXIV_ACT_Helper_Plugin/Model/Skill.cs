using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public class Skill
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string NameJa { get; set; }

        public int CriticalRate { get; set; }

        public int DirectHitRate { get; set; }

        public string[] NameList
        {
            get
            {
                return new string[] { Name, NameJa };
            }
        }
    }
}