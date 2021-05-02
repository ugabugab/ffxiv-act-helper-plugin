using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public class Actor
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        public long Hp { get; set; }
        
        public long MaxHp { get; set; }
        
        public long Mp { get; set; }
        
        public long MaxMp { get; set; }
    }
}
