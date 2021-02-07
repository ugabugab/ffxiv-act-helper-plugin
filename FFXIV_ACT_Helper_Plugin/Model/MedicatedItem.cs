using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    class MedicatedItem
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string SkillId { get; set; }

        public string BuffByte
        {
            get
            {
                if (Id != null && Id.Length >= 2)
                {
                    return Id.Substring(Id.Length - 2, 2);
                }
                return null;
            }
        }
    }
}
