using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public class Buff
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string NameJa { get; set; }

        public int Duration { get; set; }
        
        public int DamageUpRate { get; set; }

        public string[] NameList
        {
            get
            {
                return new string[] { Name, NameJa };
            }

        }
    }
}
