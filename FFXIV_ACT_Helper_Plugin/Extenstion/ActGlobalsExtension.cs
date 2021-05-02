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

        public static List<Skill> Skills = new List<Skill>();

        public static Dictionary<string, Actor> CurrentActors = new Dictionary<string, Actor>();

        public static string ConvertActNameToClientName(string name)
        {
            return (name == ActGlobals.charName) ? MyName : name;
        }

        public static string ConvertClientNameToActName(string name)
        {
            return (name == MyName) ? ActGlobals.charName : name;
        }

        public static void RunOnACTUIThread(Action code)
        {
            if (!ActGlobals.oFormActMain.InvokeRequired)
            {
                code();
                return;
            }
            if (ActGlobals.oFormActMain.IsDisposed || ActGlobals.oFormActMain.Disposing)
            {
                return;
            }
            ActGlobals.oFormActMain.Invoke(code);
        }
    }
}
