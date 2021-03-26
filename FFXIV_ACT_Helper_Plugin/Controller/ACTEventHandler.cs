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

        public void Setup()
        {
            buffSwingHistory = new List<MasterSwing>();

            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(BeforeLogLineRead);
            ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(UpdateCheckClicked);
        }

        public void Teardown()
        {
            buffSwingHistory = null;

            ActGlobals.oFormActMain.BeforeLogLineRead -= BeforeLogLineRead;
            ActGlobals.oFormActMain.UpdateCheckClicked -= UpdateCheckClicked;
        }

        void UpdateCheckClicked()
        {
            PluginMain.Shared.UpdateCheck();
        }

        void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            //Debug.WriteLine(logInfo.originalLogLine);

            try
            {
                string[] logComponents = logInfo.originalLogLine.Split('|');

                // Get character name
                // e.g. 02|2021-01-26T17:12:16.7800000+09:00|102ddfef|Hoge Fuga|a13ccee9756841e80f90f3a2498e4fd1
                if (logComponents[0] == "02")
                {
                    ActGlobalsExtension.MyName = logComponents[3];
                }

                // End combat when restarted content
                if (PluginMain.Shared.EnabledEndCombatWhenRestartContent)
                {
                    // e.g. 33|2021-01-23T16:34:42.9370000+09:00|8003757B|40000006|14E3|14|00|00|4f1194ca3def5c41059c5e69ffc7689a
                    if (logComponents[0] == "33" && logComponents[3] == "40000006")
                    {
                        if (ActGlobals.oFormActMain.InCombat)
                        {
                            // Normally EndCombat function argument should be True.
                            // see: https://advancedcombattracker.com/apidoc/html/M_Advanced_Combat_Tracker_FormActMain_EndCombat.htm
                            //ActGlobals.oFormActMain.EndCombat(false);
                            ActGlobals.oFormActMain.EndCombat(true);
                        }
                    }
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

                    // Add swings to histroy
                    // e.g. 26|2021-01-14T03:41:25.5060000+09:00|31|Medicated|30.00|102D7D99|Hoge Fuga|102D7D99|Hoge Fuga|2897|116600|116600||2cd0b18ecd384c46125530c91782c4be
                    if (logComponents[0] == "26" && logComponents[2] == "31" && logComponents[6] == logComponents[8])
                    {
                        var item = ActGlobalsExtension.MedicatedItems.Where(x => x.Id == logComponents[9]).FirstOrDefault();
                        var name = ActGlobalsExtension.ConvertClientNameToActName(logComponents[6]);

                        if (item != null
                            && (!ActGlobals.oFormActMain.InCombat
                                || ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetCombatant(name) == null))　// If target character is NOT present in active encounter
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
