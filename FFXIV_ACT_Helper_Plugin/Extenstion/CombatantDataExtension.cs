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
        private static Dictionary<string, Damage> damageCache = new Dictionary<string, Damage>();

        private class Damage
        {
            public double Value { get; set; }

            public List<BuffTakenDamage> BuffTakenDamages { get; set; }

            public MasterSwing LastSwing { get; set; }
        }

        private class BuffTakenDamage
        {
            public BuffTaken BuffTaken { get; set; }

            public double Value { get; set; }

            public MasterSwing Swing { get; set; }
        }

        private class BuffTaken
        {
            public Buff Buff { get; set; }

            public string AttackType { get; set; }

            public string Attacker { get; set; }

            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }
        }

        private static Damage GetDamage(this CombatantData data)
        {
            var job = data.GetJob();
            if (job == null || !data.IsAlly() || data.Name == null) return null;

            var key = data.Name.ToString();
            var lastSwing = data.AllOut.ContainsKey(AttackType.All) ? data.AllOut[AttackType.All].Items.LastOrDefault() : null;
            var damage = damageCache.ContainsKey(key) && damageCache[key].LastSwing == lastSwing ? damageCache[key] : null;

            if (damage == null && lastSwing != null 
                && data.Items[DamageTypeData.OutgoingDamage].Items.Count() > 0)
            {
                // Attacks
                var damageItems = data.Items[DamageTypeData.OutgoingDamage].Items[AttackType.All].Items.ToList();
                var attacks = damageItems
                    .Where(x => x.Damage.Number > 0)
                    .ToList();

                // Buff taken
                var buffTakens = new List<BuffTaken>();
                ActGlobalsExtension.Buffs.Where(x => x.Type == BuffType.Buff).ToList().ForEach(buff =>
                {
                    var conflictBuffs = ActGlobalsExtension.Buffs.Where(x => x.IsConflict(buff)).ToArray();
                    buffTakens.AddRange(data.GetBuffTakens(buff, conflictBuffs));
                });

                // Enemies debuff taken
                var enemiesDebuffTakens = new Dictionary<string, List<BuffTaken>>();
                var allies = data.Parent.GetAllies().Select(x => x.Name).ToList();
                data.Parent.Items.Select(x => x.Value).Where(x => !allies.Contains(x.Name)).ToList().ForEach(enemy =>
                {
                    var debuffTakens = new List<BuffTaken>();
                    ActGlobalsExtension.Buffs.Where(x => x.Type == BuffType.Debuff).ToList().ForEach(debuff =>
                    {
                        var conflictBuffs = ActGlobalsExtension.Buffs.Where(x => x.IsConflict(debuff)).ToArray();
                        debuffTakens.AddRange(enemy.GetBuffTakens(debuff, conflictBuffs));
                    });
                    enemiesDebuffTakens.Add(enemy.Name, debuffTakens);
                });

                var criticalRate = Math.Max(0.01 * data.CritDamPerc, 0.05);
                var directHitRate = 0.01 * data.GetDirectHitRate();

                // Damage
                var totalDamage = 0.0;
                var buffTakenDamages = new List<BuffTakenDamage>();
                attacks.ForEach(attack =>
                {
                    //if (attack.Tags.ContainsKey(SwingTag.SkillID))
                    //{
                    //    Debug.WriteLine(attack.Tags[SwingTag.SkillID] + ":" + attack.AttackType);
                    //}
                    totalDamage += attack.Damage.Number;

                    var skill = ActGlobalsExtension.Skills.Where(x => x.NameList.Contains(attack.AttackType)).FirstOrDefault();
                    var damageUpRates = new Dictionary<BuffTaken, double>();
                    var criticalUpRates = new Dictionary<BuffTaken, double>();
                    var directHitUpRates = new Dictionary<BuffTaken, double>();

                    var enemyDebuffTakens = enemiesDebuffTakens.Where(x => x.Key == attack.Victim).Select(x => x.Value).FirstOrDefault();
                    buffTakens.Concat(enemyDebuffTakens).Where(x => x.StartTime <= attack.Time && x.EndTime >= attack.Time).ToList()
                    .ForEach(buffTaken =>
                    {
                        var buff = buffTaken.Buff;

                        // Damage
                        var damageUpRate = 0.0;
                        switch (buff.Group)
                        {
                            case BuffGroup.CardForMeleeDPSOrTank:
                                damageUpRate = 0.01 * (job.IsMeleeDPSOrTank() ? 1.0 : 0.5) * buff.DamageRate;
                                break;

                            case BuffGroup.CardForRangedDPSOrHealer:
                                damageUpRate = 0.01 * (job.IsRangedDPSOrHealer() ? 1.0 : 0.5) * buff.DamageRate;
                                break;

                            case BuffGroup.Embolden:
                                if (buffTaken.Attacker == data.Name)
                                {
                                    // Apply only Magical attack
                                    damageUpRate = 0.01 * (attack.DamageType.StartsWith("Magic") ? buff.DamageRate : 0);
                                }
                                else
                                {
                                    // Apply only physical attack and decrease by time
                                    var damageRate = buff.DamageRate - (int)((attack.Time - buffTaken.StartTime).TotalSeconds / 4) * 2;
                                    damageUpRate = 0.01 * (attack.DamageType.StartsWith("Magic") ? 0 : damageRate);
                                }
                                break;

                            default:
                                damageUpRate = 0.01 * buff.DamageRate;
                                break;
                        }
                        if (damageUpRate > 0 && buffTaken.Attacker != data.Name)
                        {
                            damageUpRates.Add(buffTaken, 1.0 + damageUpRate);
                        }

                        // Critical
                        var criticalUpRate = 0.01 * buff.CriticalRate;
                        if (criticalUpRate > 0)
                        {
                            criticalUpRates.Add(buffTaken, criticalUpRate);
                        }

                        // DirectHit
                        var directHitUpRate = 0.01 * buff.DirectHitRate;
                        if (directHitUpRate > 0)
                        {
                            directHitUpRates.Add(buffTaken, directHitUpRate);
                        }
                    });

                    // Calculate DPS Taken
                    // see: https://www.fflogs.com/help/rdps

                    // M
                    var totalDamageUpRate = 1.0;
                    if (damageUpRates.Count > 0)
                    {
                        totalDamageUpRate = damageUpRates.Values.Aggregate(((y, x) => y *= x));
                    }

                    // L = N - (N / M)
                    var upDamage = attack.Damage.Number - attack.Damage.Number / totalDamageUpRate;

                    // Damage
                    if (upDamage > 0)
                    {
                        foreach (KeyValuePair<BuffTaken, double> kv in damageUpRates)
                        {
                            buffTakenDamages.Add(new BuffTakenDamage()
                            {
                                BuffTaken = kv.Key,
                                // gi = L * (log mi / log M)
                                Value = upDamage * Math.Log10(kv.Value) / Math.Log10(totalDamageUpRate),
                                Swing = attack
                            });
                        }
                    }

                    // (Cb - Cu)
                    var totalCritialUpRate = 0.0;
                    if (criticalUpRates.Count > 0)
                    {
                        totalCritialUpRate = criticalUpRates.Values.Sum();
                    }
                    if (skill != null)
                    {
                        totalCritialUpRate += 0.01 * skill.CriticalRate;
                    }

                    // (Db - Cu)
                    var totalDirectHitUpRate = 0.0;
                    if (directHitUpRates.Count > 0)
                    {
                        totalDirectHitUpRate = directHitUpRates.Values.Sum();
                    }
                    if (skill != null)
                    {
                        totalDirectHitUpRate += 0.01 * skill.DirectHitRate;
                    }

                    // Mc
                    var criticalDamageUpRate = 1.0;
                    if (attack.Critical)
                    {
                        criticalDamageUpRate = 1.4 + criticalRate - 0.05;
                    }

                    // Md
                    var directHitDamageupRate = 1.0;
                    if (attack.Tags.ContainsKey(SwingTag.DirectHit) && bool.Parse((string)attack.Tags[SwingTag.DirectHit]))
                    {
                        directHitDamageupRate = 1.25;
                    }

                    // Mdc
                    var critDHDamageUpRate = criticalDamageUpRate * directHitDamageupRate;

                    // N' = (N / M)
                    var baseDamageIncludeCDH = attack.Damage.Number / totalDamageUpRate;

                    // (N' / Mdc)
                    var baseDamage = baseDamageIncludeCDH / critDHDamageUpRate;

                    // Critical
                    // Pc = (log Mc / log Mdc) * (N' - (N' / Mdc))
                    var criticalUpDamage = Math.Log10(criticalDamageUpRate) / Math.Log10(critDHDamageUpRate) * (baseDamageIncludeCDH - baseDamage);
                    if (criticalUpDamage > 0 && totalCritialUpRate < 1.0)
                    {
                        foreach (KeyValuePair<BuffTaken, double> kv in criticalUpRates)
                        {
                            buffTakenDamages.Add(new BuffTakenDamage()
                            {
                                BuffTaken = kv.Key,
                                // gi = (ci / Cb) * Pc
                                Value = kv.Value / (totalCritialUpRate + criticalRate) * criticalUpDamage,
                                Swing = attack
                            });
                        }
                    }

                    // DirectHit
                    // Pd = (log 1.25 / log Mdc) * (N' - (N' / Mdc))
                    var directHitUpDamage = Math.Log10(directHitDamageupRate) / Math.Log10(critDHDamageUpRate) * (baseDamageIncludeCDH - baseDamage);
                    if (directHitUpDamage > 0 && totalDirectHitUpRate < 1.0)
                    {
                        foreach (KeyValuePair<BuffTaken, double> kv in directHitUpRates)
                        {
                            buffTakenDamages.Add(new BuffTakenDamage()
                            {
                                BuffTaken = kv.Key,
                                // gi = (di / Db) * Pd
                                Value = kv.Value / (totalDirectHitUpRate + directHitRate) * directHitUpDamage,
                                Swing = attack
                            });
                        }
                    }
                });

                damage = new Damage()
                {
                    Value = totalDamage,
                    BuffTakenDamages = buffTakenDamages,
                    LastSwing = lastSwing
                };

                if (damageCache.ContainsKey(key))
                {
                    damageCache[key] = damage;
                }
                else
                {
                    damageCache.Add(key, damage);
                }
            }

            return damage;
        }

        private static List<BuffTaken> GetBuffTakens(this CombatantData data, Buff buff, Buff[] conflictBuffs = null)
        {
            var buffTakens = new List<BuffTaken>();

            var buffKeys = new List<string>(buff.NameList);
            var conflictBuffKeys = (conflictBuffs != null) ? conflictBuffs.SelectMany(x => x.NameList).ToList() : new List<string>();

            var buffItems = data.Items[DamageTypeData.IncomingBuffDebuff].Items.ToList();
            //buffItems.SelectMany(x => x.Value.Items).ToList().ForEach(buffItem =>
            //{
            //    if (buffItem.Tags.ContainsKey(SwingTag.BuffID))
            //    {
            //        Debug.WriteLine(buffItem.Tags[SwingTag.BuffID] + ":" + buffItem.AttackType);
            //    }
            //});
            var swings = buffItems
            .Where(x => buffKeys.Concat(conflictBuffKeys).Contains(x.Key))
            .SelectMany(x => x.Value.Items)
            .OrderBy(x => x.Time)
            .ToList();

            MasterSwing lastSwing = null;
            foreach (var swing in swings)
            {
                if (lastSwing != null)
                {
                    var duration = Math.Min((swing.Time - lastSwing.Time).TotalSeconds, buff.Duration);
                    if ((duration < buff.Duration /*&& swing.Attacker == lastSwing.Attacker*/) // FIXME
                        && (!conflictBuffKeys.Contains(swing.AttackType) 
                            || buff.Group == BuffGroup.Embolden || buff.Group == BuffGroup.Song))
                    {
                        // skip
                    }
                    else
                    {
                        buffTakens.Add(new BuffTaken()
                        {
                            Buff = buff,
                            AttackType = lastSwing.AttackType,
                            Attacker = lastSwing.Attacker,
                            StartTime = lastSwing.Time,
                            EndTime = lastSwing.Time.AddSeconds(duration)
                        });
                        lastSwing = null;
                    }
                }
                if (swing.Tags[SwingTag.BuffID].Equals(buff.Id) /*&& swing.Attacker != data.Name*/)
                {
                    if (lastSwing == null)
                    {
                        lastSwing = swing;
                    }
                }
            }
            if (lastSwing != null)
            {
                var duration = Math.Min((data.EncEndTime - lastSwing.Time).TotalSeconds, buff.Duration);
                buffTakens.Add(new BuffTaken()
                {
                    Buff = buff,
                    AttackType = lastSwing.AttackType,
                    Attacker = lastSwing.Attacker,
                    StartTime = lastSwing.Time,
                    EndTime = lastSwing.Time.AddSeconds(duration)
                });
            }

            return buffTakens;
        }

        public static int GetMedicatedCount(this CombatantData data, bool isLastestOnly = false)
        {
            var medicatedBuffId = ActGlobalsExtension.Buffs
                .Where(x => x.Group == BuffGroup.Medicated).Select(x => x.Id).FirstOrDefault() ?? "";
            var medicatedKeys = ActGlobalsExtension.Buffs
                .Where(x => x.Id == medicatedBuffId).Select(x => x.NameList).FirstOrDefault() ?? (new string[] { });
            var medicatedBuffBytes = ActGlobalsExtension.MedicatedItems.Select(x => x.BuffByte).ToList();

            if (isLastestOnly)
            {
                // Last 5 items are the latest & high quality
                medicatedBuffBytes = medicatedBuffBytes.Skip(medicatedBuffBytes.Count - 5).Take(5).ToList();
            }

            var buffItems = data.Items[DamageTypeData.IncomingBuffDebuff].Items.ToList();
            return buffItems
                .Where(x => medicatedKeys.Contains(x.Key))
                .SelectMany(x => x.Value.Items)
                .Where(x => x.Tags.ContainsKey(SwingTag.BuffByte1) && medicatedBuffBytes.Contains(x.Tags[SwingTag.BuffByte1]))
                .Count();
        }

        public static bool IsAlly(this CombatantData data)
        {
            return bool.Parse(data.GetColumnByName("Ally"));
        }

        public static Job GetJob(this CombatantData data)
        {
            var jobName = data.GetColumnByName("Job");

            var job = new Job(jobName);
            if (job.Role != Role.Unknown || PluginDebug.EnabledUnknownJob)
            {
                return job;
            }

            return null;
        }

        public static Boss GetBoss(this CombatantData data)
        {
            var allies = data.Parent.GetAllies().Select(x => x.Name).ToList();
            var enemies = data.Parent.Items.Values.Select(x => x.Name).Where(x => !allies.Contains(x)).ToList();
            return ActGlobalsExtension.Bosses
                .Where(x => x.Zone == data.Parent.ZoneName || x.Zone == "*")
                .Where(x => x.NameList.Intersect(enemies).Count() != 0 || x.NameList.Contains("*"))
                .FirstOrDefault();
        }

        public static int GetDirectHitCount(this CombatantData data)
        {
            var damageItems = data.Items[DamageTypeData.OutgoingDamage].Items[AttackType.All].Items.ToList();
            return damageItems
                .Where(x => x.Tags.ContainsKey(SwingTag.DirectHit) && bool.Parse((string)x.Tags[SwingTag.DirectHit]))
                .Count();
        }

        public static double GetDirectHitRate(this CombatantData data)
        {
            return (double)data.GetDirectHitCount() / data.Swings * 100;
        }

        public static double GetDuration(this CombatantData data)
        {
            var boss = data.GetBoss();
            if (boss == null) return -1;

            var totalDuration = data.Parent.Duration.TotalSeconds;
            var duration = totalDuration;
            foreach (var exclusionPeriod in boss.ExclusionPeriods)
            {
                if (totalDuration > exclusionPeriod.StartTime)
                {
                    duration -= (Math.Min(totalDuration, exclusionPeriod.EndTime) - exclusionPeriod.StartTime);
                }
            }

            return duration;
        }

        public static double GetADPS(this CombatantData data)
        {
            var damage = data.GetDamage();
            var duration = data.GetDuration();
            if (damage == null || duration == -1) return -1;

            var takenDPSGroup = data.GetATakenDPSGroup();
            var takenDPS = takenDPSGroup.Values.Sum();

            return (damage.Value / duration) - takenDPS;
        }

        public static double GetRDPS(this CombatantData data)
        {
            var damage = data.GetDamage();
            var duration = data.GetDuration();
            if (damage == null || duration == -1) return -1;

            var takenDPS = data.GetRTakenDPSGroup().Values.Sum();
            var givenDPS = data.GetRGivenDPSGroup().Values.Sum();

            return (damage.Value / duration) - takenDPS + givenDPS;
        }

        public static Dictionary<Buff, double> GetATakenDPSGroup(this CombatantData data)
        {
            var damage = data.GetDamage();
            var duration = data.GetDuration();
            if (damage == null || duration == -1) return null;

            var group = new Dictionary<Buff, double>();
            damage.BuffTakenDamages.ForEach(x =>
            {
                if (x.BuffTaken.Buff.Type == BuffType.Buff
                    && x.BuffTaken.Buff.Target == BuffTarget.Solo
                    && x.BuffTaken.Attacker != data.Name)
                {
                    if (group.ContainsKey(x.BuffTaken.Buff))
                    {
                        group[x.BuffTaken.Buff] += x.Value / duration;
                    }
                    else
                    {
                        group.Add(x.BuffTaken.Buff, x.Value / duration);
                    }
                }
            });

            return group;
        }

        public static Dictionary<string, double> GetRTakenDPSGroup(this CombatantData data)
        {
            var damage = data.GetDamage();
            var duration = data.GetDuration();
            if (damage == null || duration == -1) return null;

            var group = new Dictionary<string, double>();
            damage.BuffTakenDamages.ForEach(x =>
            {
                if (x.BuffTaken.Attacker != data.Name)
                {
                    if (group.ContainsKey(x.BuffTaken.AttackType))
                    {
                        group[x.BuffTaken.AttackType] += x.Value / duration;
                    }
                    else
                    {
                        group.Add(x.BuffTaken.AttackType, x.Value / duration);
                    }
                }
            });

            return group;
        }

        public static Dictionary<string, double> GetRGivenDPSGroup(this CombatantData data)
        {
            var duration = data.GetDuration();
            if (duration == -1) return null;

            var group = new Dictionary<string, double>();
            var allies = data.Parent.GetAllies().Where(x => x.Name != data.Name).ToList();
            allies.ForEach(ally =>
            {
                var allyDamage = ally.GetDamage();
                if (allyDamage != null)
                {
                    allyDamage.BuffTakenDamages.OrderBy(x => x.Swing.Time).ToList().ForEach(x =>
                    {
                        if (x.BuffTaken.Attacker == data.Name && x.Value > 0)
                        {
                            if (group.ContainsKey(x.BuffTaken.AttackType))
                            {
                                group[x.BuffTaken.AttackType] += x.Value / duration;
                            }
                            else
                            {
                                group.Add(x.BuffTaken.AttackType, x.Value / duration);
                            }
                        }
                    });
                }
            });

            return group;
        }

        public static int GetAPerf(this CombatantData data)
        {
            var boss = data.GetBoss();
            var job = data.GetJob();
            if (boss == null || job == null) return -1;

            var aDPS = data.GetADPS();
            var aPercentile = boss.APercentiles.Where(x => x.Job == job.Name).FirstOrDefault();
            if (aDPS == -1 || aPercentile == null)
            {
                return -1;
            }

            return CalculatePerf(aDPS, aPercentile);
        }

        public static int GetRPerf(this CombatantData data)
        {
            var boss = data.GetBoss();
            var job = data.GetJob();
            if (boss == null || job == null) return -1;

            var rDPS = data.GetRDPS();
            var rPercentile = boss.RPercentiles.Where(x => x.Job == job.Name).FirstOrDefault();
            if (rDPS == -1 || rPercentile == null)
            {
                return -1;
            }

            return CalculatePerf(rDPS, rPercentile);
        }

        private static int CalculatePerf(double dps, Percentile percentile)
        {
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

            var perf = 1.0;

            if (d0 != d1) // Avoid division by zero
            {
                perf = p0 + ((dps - d0) / ((d1 - d0) / (p1 - p0)));
                perf = Math.Round(perf, MidpointRounding.AwayFromZero);
                perf = Math.Max(perf, 1.0);
            }

            return (int)perf;
        }
    }
}
