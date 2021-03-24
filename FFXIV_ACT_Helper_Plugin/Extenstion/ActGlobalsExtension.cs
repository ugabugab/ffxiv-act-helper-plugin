using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class ActGlobalsExtension
    {
        public static string MyName = ActGlobals.charName;

        public static List<Boss> Bosses = new List<Boss>();

        public static List<MedicatedItem> MedicatedItems = new List<MedicatedItem>();

        public static List<Buff> Buffs = new List<Buff>();

        public static string ConvertActNameToClientName(string name)
        {
            return (name == ActGlobals.charName) ? MyName : name;
        }

        public static string ConvertClientNameToActName(string name)
        {
            return (name == MyName) ? ActGlobals.charName : name;
        }
    }
}
