using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class ActGlobalsExtension
    {
        public static string myName = ActGlobals.charName;

        public static List<Boss> bosses = new List<Boss>();

        public static List<MedicatedItem> medicatedItems = new List<MedicatedItem>();

        public static List<Buff> buffs = new List<Buff>();

        public static string ConvertActNameToClientName(string name)
        {
            return (name == ActGlobals.charName) ? myName : name;
        }

        public static string ConvertClientNameToActName(string name)
        {
            return (name == myName) ? ActGlobals.charName : name;
        }
    }
}
