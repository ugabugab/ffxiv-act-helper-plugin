using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;
using System.Diagnostics;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class CombatantDataExtension
    {
        public static int GetMedicatedCount(this CombatantData data, bool isLastestOnly = false)
        {
            var medicatedBuff = ActGlobalsExtension.GetMedicatedBuff();
            var medicatedKeys = ActGlobalsExtension.buffs
                .Where(x => x.Id == medicatedBuff.Id).Select(x => x.NameList).FirstOrDefault() ?? (new string[] { });
            var medicatedBuffBytes = ActGlobalsExtension.medicatedItems.Select(x => x.BuffByte).ToList();

            if (isLastestOnly)
            {
                // Last 5 items are the latest & high quality
                medicatedBuffBytes = medicatedBuffBytes.Skip(medicatedBuffBytes.Count - 5).Take(5).ToList();
            }

            return data.AllInc
                .Where(x => medicatedKeys.Contains(x.Key))
                .Select(x => x.Value.Items
                    .Where(y => y.Tags.ContainsKey("BuffByte1") && medicatedBuffBytes.Contains(y.Tags["BuffByte1"]))
                    .Count())
                .Sum();
        }

        public static Job GetJob(this CombatantData data)
        {
            var jobName = data.AllOut
                .SelectMany(x => x.Value.Items)
                .Where(x => x.Tags.ContainsKey("Job"))
                .Select(x => x.Tags["Job"].ToString())
                .FirstOrDefault() ?? data.GetColumnByName("Job"); // If failed to get Job from Tag, get it from Column

            if (jobName != null && jobName.Length != 0)
            {
                return new Job(jobName);
            }

            return  null;
        }

        public static Boss GetBoss(this CombatantData data)
        {
            return ActGlobalsExtension.bosses
                .Where(x => x.Zone == data.Parent.ZoneName)
                .Where(x => x.NameList.Intersect(data.Allies.Keys).Count() != 0)
                .FirstOrDefault();
        }

        public static double GetTotalBuffDuration(this CombatantData data, Buff buff, Buff[] conflictBuffs = null)
        {
            var totalDuration = 0.0;

            var buffKeys = new List<string>(buff.NameList);
            if (conflictBuffs != null)
            {
                buffKeys.AddRange(conflictBuffs.SelectMany(x => x.NameList));
            }

            var swings = data.AllInc
                .Where(x => buffKeys.Contains(x.Key))
                .SelectMany(x => x.Value.Items)
                .OrderBy(x => x.Time)
                .ToList();

            MasterSwing lastSwing = null;
            foreach (var swing in swings)
            {
                if (lastSwing != null)
                {
                    var duration = Math.Min((swing.Time - lastSwing.Time).TotalSeconds, buff.Duration);
                    totalDuration += duration;
                    lastSwing = null;
                }
                if (swing.Tags["BuffID"].Equals(buff.Id) && swing.Attacker != data.Name)
                {
                    lastSwing = swing;
                }
            }
            if (lastSwing != null)
            {
                var duration = Math.Min((data.EncEndTime - lastSwing.Time).TotalSeconds, buff.Duration);
                totalDuration += duration;
            }

            return totalDuration;
        }

        public static double GetADPS(this CombatantData data, Job job, Boss boss)
        {
            var totalDuration = data.Parent.Duration.TotalSeconds;
            var damage = data.Damage;

            // Add pet's DPS
            var name = ActGlobalsExtension.ConvertActNameToClientName(data.Name);
            damage += data.Parent.Items
                .Where(x => x.Value.Name.Contains("(" + name + ")"))
                .Select(x => x.Value.Damage)
                .Sum();

            // Subtract the damage up by specific buffs
            var upRate = 0.0;

            var cardBuffs = ActGlobalsExtension.GetCardBuffs().ToArray();
            ActGlobalsExtension.GetDamageBuffs().ForEach(buff =>
            {
                switch(buff.Group)
                {
                    case BuffGroup.CardForMeleeDPSOrTank:
                        upRate += 0.01 * (job.IsMeleeDPSOrTank() ? 1.0 : 0.5) * buff.Value * data.GetTotalBuffDuration(buff, cardBuffs) / totalDuration;
                        break;

                    case BuffGroup.CardForRangedDPSOrHealer:
                        upRate += 0.01 * (job.IsRangedDPSOrHealer() ? 1.0 : 0.5) * buff.Value * data.GetTotalBuffDuration(buff, cardBuffs) / totalDuration;
                        break;

                    default:
                        upRate += 0.01 * buff.Value * data.GetTotalBuffDuration(buff) / totalDuration;
                        break;
                }
            });

            // Apply exclusion period
            var duration = totalDuration;
            foreach (var exclusionPeriod in boss.ExclusionPeriods)
            {
                if (totalDuration > exclusionPeriod.StartTime)
                {
                    duration -= (Math.Min(totalDuration, exclusionPeriod.EndTime) - exclusionPeriod.StartTime);
                }
            }

            return damage / (1.0 + upRate) / duration;
        }

        public static int GetPerf(this CombatantData data, Job job, Boss boss)
        {
            var perf = 1.0;

            var percentile = boss.Percentiles.Where(x => x.Job == job.Name).FirstOrDefault();
            if (percentile != null)
            {
                var dps = data.GetADPS(job, boss);

                var perfTable = new Dictionary<int, int>()
                {
                    { 99, percentile.Perf99 },
                    { 95, percentile.Perf95 },
                    { 75, percentile.Perf75 },
                    { 50, percentile.Perf50 },
                    { 25, percentile.Perf25 },
                    { 10, percentile.Perf10 },
                    { 1, percentile.Perf1 },
                };

                var p0 = 0.0;
                var p1 = 100.0;
                var d0 = 0.0;
                var d1 = Double.MaxValue;

                foreach (var x in perfTable)
                {
                    if (dps >= x.Value)
                    {
                        p0 = x.Key;
                        d0 = x.Value;
                        break;
                    }
                    p1 = x.Key;
                    d1 = x.Value;
                }
                
                if (d0 != d1) // Avoid division by zero
                {
                    perf = p0 + ((dps - d0) / ((d1 - d0) / (p1 - p0)));
                    perf = Math.Round(perf, MidpointRounding.AwayFromZero);
                    perf = Math.Max(perf, 1.0);
                }
            }

            return (int)perf;
        }
    }
}
