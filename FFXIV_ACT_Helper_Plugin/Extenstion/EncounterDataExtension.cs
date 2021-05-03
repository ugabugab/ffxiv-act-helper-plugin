using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class EncounterDataExtension
    {
        public static Boss GetBoss(this EncounterData data)
        {
            var allies = data.GetAllies().Select(x => x.Name).ToList();
            var enemies = data.Items.Values.Select(x => x.Name).Where(x => !allies.Contains(x)).ToList();
            return ActGlobalsExtension.Bosses
                .Where(x => x.Zone == data.ZoneName || x.Zone == "*")
                .Where(x => x.NameList.Intersect(enemies).Count() != 0 || x.NameList.Contains("*"))
                .FirstOrDefault();
        }

        public static TimeSpan GetBossDuration(this EncounterData data)
        {
            var startTime = data.StartTime;
            var endTime = data.Tags.ContainsKey(EncounterTag.EndTime) ? (DateTime)data.Tags[EncounterTag.EndTime] : data.EndTime;
            var totalDuration = (endTime - startTime).TotalSeconds;
            var duration = totalDuration;

            var boss = data.GetBoss();
            if (boss != null)
            {
                foreach (var exclusionPeriod in boss.ExclusionPeriods)
                {
                    if (totalDuration > exclusionPeriod.StartTime)
                    {
                        duration -= (Math.Min(totalDuration, exclusionPeriod.EndTime) - exclusionPeriod.StartTime);
                    }
                }
            }

            return TimeSpan.FromSeconds(duration);
        }
    }
}
