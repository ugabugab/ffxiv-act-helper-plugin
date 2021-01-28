using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public partial class PluginMain : UserControl, IActPluginV1
    {
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\FFXIV_ACT_Helper_Plugin.config.xml");
        SettingsSerializer xmlSettings;

        string myName;
        List<MasterSwing> buffSwingHistory = new List<MasterSwing>();

        bool EnabledEndCombatWhenRestartContent
        {
            get
            {
                return this.checkBox1.Checked;
            }
        }

        bool EnabledDetectBuffsDuringNonCombat
        {
            get
            {
                return this.checkBox2.Checked;
            }
        }

        bool EnabledCountMedicatedBuffs
        {
            get
            {
                return this.checkBox2.Checked;
            }
        }

        public PluginMain()
        {
            InitializeComponent();
        }

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= BeforeLogLineRead;

            SaveSettings();
            lblStatus.Text = "Plugin Exited";
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
            pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
            this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();
            UpdateACTTables();

            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(BeforeLogLineRead);

            lblStatus.Text = "Plugin Started";
        }

        void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            Debug.WriteLine(logInfo.originalLogLine);

            try
            {
                string[] logComponents = logInfo.originalLogLine.Split('|');

                // Get character name
                // e.g. 02|2021-01-26T17:12:16.7800000+09:00|102ddfef|Hoge Fuga|a13ccee9756841e80f90f3a2498e4fd1
                if (logComponents[0] == "02")
                {
                    this.myName = logComponents[3];
                }

                // End combat when restarted content
                if (this.EnabledEndCombatWhenRestartContent)
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
                if (this.EnabledDetectBuffsDuringNonCombat)
                {
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        // Insert swings to ACT
                        foreach (var swing in Enumerable.Reverse(buffSwingHistory))
                        {
                            MasterSwing newSwing = new MasterSwing(
                                swing.SwingType,
                                swing.Critical,
                                swing.Damage,
                                DateTime.Now,
                                swing.TimeSorter,
                                swing.AttackType,
                                swing.Attacker,
                                swing.DamageType,
                                swing.Victim);
                            newSwing.Tags = swing.Tags;
                            newSwing.Tags["BuffDuration"] = (double)swing.Tags["BuffDuration"] - (DateTime.Now - swing.Time).Ticks / TimeSpan.TicksPerSecond;

                            ActGlobals.oFormActMain.AddCombatAction(newSwing);

                            buffSwingHistory.Remove(swing);
                        }
                    }
                    else
                    {
                        // Add swings to histroy
                        // e.g. 26|2021-01-14T03:41:25.5060000+09:00|31|Medicated|30.00|102D7D99|Hoge Fuga|102D7D99|Hoge Fuga|2897|116600|116600||2cd0b18ecd384c46125530c91782c4be
                        if (logComponents[0] == "26" && logComponents[2] == "31" && logComponents[6] == logComponents[8])
                        {
                            string name = (this.myName == logComponents[6] ? ActGlobals.charName : logComponents[6]);

                            MasterSwing swing = new MasterSwing(21, false, Dnum.Unknown, DateTime.Parse(logComponents[1]), 0, logComponents[3], name, "", name);
                            //swing.Tags.Add("Potency", 0);
                            //swing.Tags.Add("Job", "");
                            //swing.Tags.Add("ActorID", logComponents[5]);
                            //swing.Tags.Add("TargetID", logComponents[7]);
                            //swing.Tags.Add("SkillID", "34578697"); 
                            //swing.Tags.Add("BuffID", "49");
                            swing.Tags.Add("BuffDuration", double.Parse(logComponents[4]));
                            //swing.Tags.Add("BuffByte1", "97");
                            //swing.Tags.Add("BuffByte2", "00");
                            //swing.Tags.Add("BuffByte3", "00");

                            buffSwingHistory.Add(swing);
                        }

                        // Remove expired swings
                        foreach (var swing in Enumerable.Reverse(buffSwingHistory))
                        {
                            if ((DateTime.Now - swing.Time).Ticks / TimeSpan.TicksPerSecond > (double)swing.Tags["BuffDuration"])
                            {
                                buffSwingHistory.Remove(swing);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void CheckedChanged(object sender, EventArgs e)
        {
            UpdateACTTables();
        }

        void LoadSettings()
        {
            // Add any controls you want to save the state of
            xmlSettings.AddControlSetting(checkBox1.Name, checkBox1);
            xmlSettings.AddControlSetting(checkBox2.Name, checkBox2);

            if (File.Exists(settingsFile))
            {
                FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XmlTextReader xReader = new XmlTextReader(fs);

                try
                {
                    while (xReader.Read())
                    {
                        if (xReader.NodeType == XmlNodeType.Element)
                        {
                            if (xReader.LocalName == "SettingsSerializer")
                            {
                                xmlSettings.ImportFromXml(xReader);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error loading settings: " + ex.Message;
                }
                xReader.Close();
            }
        }

        void SaveSettings()
        {
            FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            xWriter.Indentation = 1;
            xWriter.IndentChar = '\t';
            xWriter.WriteStartDocument(true);
            xWriter.WriteStartElement("Config");    // <Config>
            xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
            xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
            xWriter.WriteEndElement();  // </SettingsSerializer>
            xWriter.WriteEndElement();  // </Config>
            xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
            xWriter.Flush();    // Flush the file buffer to disk
            xWriter.Close();
        }

        void UpdateACTTables()
        {
            if (this.EnabledCountMedicatedBuffs)
            {
                if (!CombatantData.ColumnDefs.ContainsKey("MedicatedCount"))
                {
                    CombatantData.ColumnDefs.Add(
                        "MedicatedCount",
                        new CombatantData.ColumnDef(
                            "MedicatedCount",
                            true,
                            "INT",
                            "MedicatedCount",
                            new CombatantData.StringDataCallback(MedicatedCountDataCallback),
                            new CombatantData.StringDataCallback(MedicatedCountDataCallback),
                            new Comparison<CombatantData>(MedicatedCountSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("MedicatedCount"))
                {
                    CombatantData.ExportVariables.Add(
                        "MedicatedCount",
                        new CombatantData.TextExportFormatter(
                            "MedicatedCount",
                            "MedicatedCount",
                            "Number of medicated buffs",
                            new CombatantData.ExportStringDataCallback(MedicatedCountExporttDataCallback)));
                }
            }
            else
            {
                if (CombatantData.ColumnDefs.ContainsKey("MedicatedCount"))
                {
                    CombatantData.ColumnDefs.Remove("MedicatedCount");
                }
                if (CombatantData.ExportVariables.ContainsKey("MedicatedCount"))
                {
                    CombatantData.ExportVariables.Remove("MedicatedCount");
                }
            }
            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
        }

        string MedicatedCountDataCallback(CombatantData data)
        {
            return data.AllInc
                .Where(x => x.Key == "Medicated" || x.Key == "強化薬")  // TODO: localize
                .Select(x => x.Value.Swings.ToString())
                .FirstOrDefault() ?? "0";
        }

        int MedicatedCountSortComparer(CombatantData left, CombatantData right)
        {
            return (int.Parse(left.GetColumnByName("MedicatedCount")) <= int.Parse(right.GetColumnByName("MedicatedCount"))) ? -1 : 1;
        }

        string MedicatedCountExporttDataCallback(CombatantData Data, string ExtraFormat)
        {
            return Data.GetColumnByName("MedicatedCount");
        }
    }
}
