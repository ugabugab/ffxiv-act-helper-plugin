using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    class ACTEventHandler
    {
        List<MasterSwing> buffSwingHistory;
        
        string[] emergencyTacticsNames = { "Emergency Tactics", "応急戦術" };
        string[] e12sBossNames = { "Eden's Promise", "プロミス・オブ・エデン" };

        public void Setup()
        {
            this.buffSwingHistory = new List<MasterSwing>();

            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(BeforeLogLineRead);
            ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(UpdateCheckClicked);
            ActGlobals.oFormActMain.AfterCombatAction += new CombatActionDelegate(AfterCombatAction);
            ActGlobals.oFormActMain.OnCombatStart += new CombatToggleEventDelegate(OnCombatStart);
        }

        public void Teardown()
        {
            this.buffSwingHistory = null;

            ActGlobals.oFormActMain.BeforeLogLineRead -= BeforeLogLineRead;
            ActGlobals.oFormActMain.UpdateCheckClicked -= UpdateCheckClicked;
            ActGlobals.oFormActMain.AfterCombatAction -= AfterCombatAction;
            ActGlobals.oFormActMain.OnCombatStart -= OnCombatStart;
        }

        void UpdateCheckClicked()
        {
            PluginMain.Shared.UpdateCheck();
        }

        void OnCombatStart(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            ActGlobalsExtension.CurrentActors.Clear();
            CombatantDataExtension.ClearCache();
        }

        void AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {
            if (PluginMain.Shared.EnabledSimulateFFLogsParses)
            {
                var targetId = (string)actionInfo.tags.GetValue(SwingTag.TargetID);
                var target = ActGlobalsExtension.CurrentActors.GetValue(targetId);
                if (target != null && !actionInfo.cancelAction)
                {
                    // Damage Action
                    if (actionInfo.swingType == SwingType.Attack || actionInfo.swingType == SwingType.DamageSkill || actionInfo.swingType == SwingType.Dot)
                    {
                        var damage = actionInfo.damage.Number;
                        var overkill = Math.Min(damage, Math.Max(damage - target.Hp, 0));
                        if (overkill > 0)
                        {
                            actionInfo.combatAction.Tags[SwingTag.Overkill] = overkill.ToString();
                        }
                        else
                        {
                            actionInfo.combatAction.Tags.Remove(SwingTag.Overkill);
                        }
                        target.Hp -= (damage - overkill);
                        ActGlobalsExtension.CurrentActors[targetId] = target;
                    }
                    // Healing Action
                    if (actionInfo.swingType == SwingType.HealSkill || actionInfo.swingType == SwingType.Hot)
                    {
                        var healing = actionInfo.damage.Number;
                        var overheal = 0L;
                        if (actionInfo.theDamageType != DamageType.DamageShield && actionInfo.theDamageType != DamageType.Absorb)
                        {
                            overheal = Math.Min(healing, Math.Max((target.Hp + healing) - target.MaxHp, 0));
                        }
                        if (overheal > 0)
                        {
                            actionInfo.combatAction.Tags[SwingTag.Overheal] = overheal.ToString();
                        }
                        else
                        {
                            actionInfo.combatAction.Tags.Remove(SwingTag.Overheal);
                        }
                        if (actionInfo.theDamageType != DamageType.DamageShield)
                        {
                            target.Hp += (healing - overheal);
                            ActGlobalsExtension.CurrentActors[targetId] = target;
                        }

                        // support for Emergency Tactics
                        if (emergencyTacticsNames.Contains(actionInfo.theAttackType))
                        {
                            var emergencyTacticsBuff = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetAllies()
                                .SelectMany(x => x.Items[DamageTypeData.OutgoingBuffDebuff].Items)
                                .Where(x => emergencyTacticsNames.Contains(x.Key))
                                .SelectMany(x => x.Value.Items)
                                .OrderByDescending(x => x.Time).FirstOrDefault();
                            if (emergencyTacticsBuff != null && actionInfo.theDamageType == DamageType.Absorb)
                            {
                                var origin = actionInfo.combatAction;

                                // Add new swing
                                var swing = new MasterSwing(
                                    origin.SwingType,
                                    origin.Critical,
                                    origin.Damage.Number,
                                    origin.Time,
                                    origin.TimeSorter,
                                    origin.AttackType,
                                    emergencyTacticsBuff.Attacker,
                                    "",
                                    origin.Victim);
                                swing.Tags.Add(SwingTag.Job, emergencyTacticsBuff.Tags.GetValue(SwingTag.Job));
                                swing.Tags.Add(SwingTag.ActorID, emergencyTacticsBuff.Tags.GetValue(SwingTag.ActorID));
                                swing.Tags.Add(SwingTag.TargetID, origin.Tags.GetValue(SwingTag.TargetID));
                                swing.Tags.Add(SwingTag.Overheal, origin.Tags.GetValue(SwingTag.Overheal));
                                ActGlobals.oFormActMain.AddCombatAction(swing);

                                origin.Damage.SetNum(0);
                                origin.Tags[SwingTag.Overheal] = 0;
                            }
                        }
                    }
                }
            }
        }

        void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            //Debug.WriteLine(logInfo.originalLogLine);
            //Debug.WriteLine(logInfo.logLine);

            try
            {
                string[] logComponents = logInfo.originalLogLine.Split('|');

                if (logComponents[0] == "00")
                {
                }
                // e.g. 02|2021-01-26T17:12:16.7800000+09:00|102ddfef|Hoge Fuga|a13ccee9756841e80f90f3a2498e4fd1
                else if (logComponents[0] == "02")
                {
                    // Get character name
                    ActGlobalsExtension.MyName = logComponents[3];
                }
                // e.g. 21|2021-04-24T21:09:58.0530000+09:00|1029D1FC|Hoge Fuga|8CF|Aeolian Edge|400038E8|Eden's Promise|4F710203|74390000|53D|9F8000|53D|9F8000|11B|8CF8000|0|0|0|0|0|0|0|0|26289953|63981880|0|10000|0|1000|-0.01531982|-75.02869|75|3.13421|90796|148446|10000|10000|0|1000|3.017035|-71.21938|74.99991|0.4098789|0000105C|0|e1d390585909d0237d8553a257999349
                else if (logComponents[0] == "21")
                {
                    //// Upate actors
                    //var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    //actor.Id = logComponents[2];
                    //actor.Name = logComponents[3];
                    //if (long.TryParse(logComponents[34], out long actorHp)) actor.Hp = actorHp;
                    //if (long.TryParse(logComponents[35], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    //if (long.TryParse(logComponents[36], out long actorMp)) actor.Mp = actorMp;
                    //if (long.TryParse(logComponents[37], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    //ActGlobalsExtension.CurrentActors[actor.Id] = actor;

                    //var target = ActGlobalsExtension.CurrentActors.GetValue(logComponents[6], new Actor());
                    //target.Id = logComponents[6];
                    //target.Name = logComponents[7];
                    //if (long.TryParse(logComponents[24], out long targetHp)) target.Hp = targetHp;
                    //if (long.TryParse(logComponents[25], out long targetMaxHp)) target.MaxHp = targetMaxHp;
                    //if (long.TryParse(logComponents[26], out long targetMp)) target.Mp = targetMp;
                    //if (long.TryParse(logComponents[27], out long targetMaxMp)) target.MaxMp = targetMaxMp;
                    //ActGlobalsExtension.CurrentActors[target.Id] = target;
                }
                // e.g. 22|2021-04-24T21:33:58.2180000+09:00|1027A809|Hoge Fuga|83|Cure III|10329758|Hoge Fuga|10004|FD800000|1B|838000|0|0|0|0|0|0|0|0|0|0|0|0|8854|148314|10000|10000|0|1000|0.1677856|-69.71857|75|-0.05412173|7269|134545|9925|10000|0|1000|-0.3510132|-69.0166|75|0.1415596|00002F96|1|eeabc872634d29aea376df30c9faf4d4
                else if (logComponents[0] == "22")
                {
                    //// Upate actors
                    //var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    //actor.Id = logComponents[2];
                    //actor.Name = logComponents[3];
                    //if (long.TryParse(logComponents[34], out long actorHp)) actor.Hp = actorHp;
                    //if (long.TryParse(logComponents[35], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    //if (long.TryParse(logComponents[36], out long actorMp)) actor.Mp = actorMp;
                    //if (long.TryParse(logComponents[37], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    //ActGlobalsExtension.CurrentActors[actor.Id] = actor;

                    //var target = ActGlobalsExtension.CurrentActors.GetValue(logComponents[6], new Actor());
                    //target.Id = logComponents[6];
                    //target.Name = logComponents[7];
                    //if (long.TryParse(logComponents[24], out long targetHp)) target.Hp = targetHp;
                    //if (long.TryParse(logComponents[25], out long targetMaxHp)) target.MaxHp = targetMaxHp;
                    //if (long.TryParse(logComponents[26], out long targetMp)) target.Mp = targetMp;
                    //if (long.TryParse(logComponents[27], out long targetMaxMp)) target.MaxMp = targetMaxMp;
                    //ActGlobalsExtension.CurrentActors[target.Id] = target;
                }
                // e.g. 24|2021-04-24T21:09:58.0530000+09:00|102CEDF1|Hoge Fuga|HoT|0|1335|71559|134501|8000|10000|0|1000|10.58232|-85.01506|74.99805|-2.90611||0866bc95d8ca26aefe03700f6d4d428e
                else if (logComponents[0] == "24")
                {
                    //// Upate actors
                    //var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    //actor.Id = logComponents[2];
                    //actor.Name = logComponents[3];
                    //if (long.TryParse(logComponents[7], out long actorHp)) actor.Hp = actorHp;
                    //if (long.TryParse(logComponents[8], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    //if (long.TryParse(logComponents[9], out long actorMp)) actor.Mp = actorMp;
                    //if (long.TryParse(logComponents[10], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    //ActGlobalsExtension.CurrentActors[actor.Id] = actor;
                }
                // e.g. 26|2021-01-14T03:41:25.5060000+09:00|31|Medicated|30.00|102D7D99|Hoge Fuga|102D7D99|Hoge Fuga|2897|116600|116600||2cd0b18ecd384c46125530c91782c4be
                else if (logComponents[0] == "26")
                {
                    if (PluginMain.Shared.EnabledDetectBuffsDuringNonCombat)
                    {
                        // Add swings to history
                        if (logComponents[2] == "31" && logComponents[6] == logComponents[8])
                        {
                            var item = ActGlobalsExtension.MedicatedItems.Where(x => x.Id == logComponents[9]).FirstOrDefault();
                            var name = ActGlobalsExtension.ConvertClientNameToActName(logComponents[6]);

                            if (item != null
                                && (!ActGlobals.oFormActMain.InCombat
                                    || ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetCombatant(name) == null)) // If target character is NOT present in active encounter
                            {
                                var medicatedBuffId = ActGlobalsExtension.Buffs
                                    .Where(x => x.Group == BuffGroup.Medicated).Select(x => x.Id).FirstOrDefault() ?? "";

                                MasterSwing swing = new MasterSwing(SwingType.Buff, false, Dnum.Unknown, DateTime.Parse(logComponents[1]), 0, logComponents[3], name, "", name);
                                swing.Tags.Add(SwingTag.Potency, 0);
                                //swing.Tags.Add("Job", "");
                                swing.Tags.Add(SwingTag.ActorID, logComponents[5]);
                                swing.Tags.Add(SwingTag.TargetID, logComponents[7]);
                                swing.Tags.Add(SwingTag.SkillID, item.SkillId);
                                swing.Tags.Add(SwingTag.BuffID, medicatedBuffId);
                                swing.Tags.Add(SwingTag.BuffDuration, double.Parse(logComponents[4]));
                                swing.Tags.Add(SwingTag.BuffByte1, item.BuffByte);
                                swing.Tags.Add(SwingTag.BuffByte2, "00");
                                swing.Tags.Add(SwingTag.BuffByte3, "00");

                                buffSwingHistory.Add(swing);
                            }
                        }
                    }
                }
                // e.g. 33|2021-01-23T16:34:42.9370000+09:00|8003757B|40000006|14E3|14|00|00|4f1194ca3def5c41059c5e69ffc7689a
                else if (logComponents[0] == "33")
                {
                    if (PluginMain.Shared.EnabledEndCombatWhenRestartContent)
                    {
                        if (ActGlobals.oFormActMain.InCombat && logComponents[3] == "40000006")
                        {
                            ActGlobals.oFormActMain.EndCombat(true);
                        }
                    }
                }
                // e.g. 36|2021-04-03T00:56:34.5320000+09:00|0000|2|6b09eaac147276ef6797f3ebe8b87cec
                else if (logComponents[0] == "36")
                {
                    if (PluginMain.Shared.EnabledEndCombatWhenRestartContent)
                    {
                        // Support for E12S
                        if (ActGlobals.oFormActMain.InCombat && logComponents[2] == "0000" && logComponents[3] == "2")
                        {
                            if (ActGlobalsExtension.CurrentActors.Values.Where(x => e12sBossNames.Contains(x.Name) && x.Hp <= 1).Any())
                            {
                                ActGlobals.oFormActMain.EndCombat(true);
                            }
                        }
                    }
                }
                // e.g. 37|2021-04-24T21:06:22.5290000+09:00|400038E8|Hoge Fuga|000009CA|50301450|63981880|0|10000|0||-0.01531982|-75.02869|75|-3.127499||913874a8e32f15768231b642f7ce089f
                else if (logComponents[0] == "37")
                {
                    // Upate actors
                    var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    actor.Id = logComponents[2];
                    actor.Name = logComponents[3];
                    if (long.TryParse(logComponents[5], out long actorHp)) actor.Hp = actorHp;
                    if (long.TryParse(logComponents[6], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    if (long.TryParse(logComponents[7], out long actorMp)) actor.Mp = actorMp;
                    if (long.TryParse(logComponents[8], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    ActGlobalsExtension.CurrentActors[actor.Id] = actor;

                    // Support for E12S
                    if (e12sBossNames.Contains(actor.Name) && actor.Hp <= 1)
                    {
                        if (!ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Tags.ContainsKey(EncounterTag.EndTime))
                        {
                            ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Tags[EncounterTag.EndTime] = logInfo.detectedTime;
                        }
                    }
                }
                // e.g. 38|2021-04-24T23:20:37.4140000+09:00|1024B79F|Hoge Fuga|004A4A25|88344|88344|10000|10000|197|0|539.3408|322.6061|-19.50564|-2.613231|0600|70|0||da6f6b8f2442147780d67917209e55ef
                else if (logComponents[0] == "38")
                {
                    // Upate actors
                    var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    actor.Id = logComponents[2];
                    actor.Name = logComponents[3];
                    if (long.TryParse(logComponents[5], out long actorHp)) actor.Hp = actorHp;
                    if (long.TryParse(logComponents[6], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    if (long.TryParse(logComponents[7], out long actorMp)) actor.Mp = actorMp;
                    if (long.TryParse(logComponents[8], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    ActGlobalsExtension.CurrentActors[actor.Id] = actor;
                }
                // e.g. 39|2021-04-24T21:06:22.4850000+09:00|400038ED|Hoge Fuga|127121|127121|10000|10000|0|0|-0.04577637|-75.08972|75|0.2127874||0999f8bf116f6727c045b0d6c88dc848
                else if (logComponents[0] == "39")
                {
                    // Upate actors
                    var actor = ActGlobalsExtension.CurrentActors.GetValue(logComponents[2], new Actor());
                    actor.Id = logComponents[2];
                    actor.Name = logComponents[3];
                    if (long.TryParse(logComponents[4], out long actorHp)) actor.Hp = actorHp;
                    if (long.TryParse(logComponents[5], out long actorMaxHp)) actor.MaxHp = actorMaxHp;
                    if (long.TryParse(logComponents[6], out long actorMp)) actor.Mp = actorMp;
                    if (long.TryParse(logComponents[7], out long actorMaxMp)) actor.MaxMp = actorMaxMp;
                    ActGlobalsExtension.CurrentActors[actor.Id] = actor;
                }

                // Detect medicated buff during non-combat
                // TODO: Apply all buffs
                if (PluginMain.Shared.EnabledDetectBuffsDuringNonCombat)
                {
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        // Insert swings to ACT
                        foreach (var swing in Enumerable.Reverse(buffSwingHistory))
                        {
                            swing.Tags[SwingTag.BuffDuration] = (double)swing.Tags[SwingTag.BuffDuration] - (DateTime.Now - swing.Time).Ticks / TimeSpan.TicksPerSecond;
                            ActGlobals.oFormActMain.AddCombatAction(swing);

                            buffSwingHistory.Remove(swing);
                        }
                    }

                    // Remove expired swings
                    foreach (var swing in Enumerable.Reverse(buffSwingHistory))
                    {
                        if ((DateTime.Now - swing.Time).Ticks / TimeSpan.TicksPerSecond > (double)swing.Tags[SwingTag.BuffDuration])
                        {
                            buffSwingHistory.Remove(swing);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
