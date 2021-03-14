using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class CombatantDataExtension
    {
        public static string GetJob(this CombatantData data)
        {
            var job = data.AllOut
                .Select(x => x.Value.Items
                    .Where(y => y.Tags.ContainsKey("Job"))
                    .Select(y => y.Tags["Job"].ToString())
                    .FirstOrDefault())
                .FirstOrDefault();

            // If failed to get Job from Tag, get it from Column
            if (job == null)
            {
                job = data.GetColumnByName("Job");
            }

            return job;
        }

        public static double GetADPS(this CombatantData data)
        {
            var dps = data.EncDPS;

            // Add pet's DPS
            var name = ActGlobalsExtension.ConvertActNameToClientName(data.Name);
            dps += data.Parent.Items
                .Where(x => x.Value.Name.Contains("(" + name + ")"))
                .Select(x => x.Value.EncDPS)
                .Sum();

            // TODO: Subtract the DPS boosted by specific buffs
            var job = data.GetJob();

            return dps;
        }
    }
}
