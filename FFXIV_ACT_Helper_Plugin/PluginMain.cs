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
using System.Net;
using System.Xml.Serialization;

namespace FFXIV_ACT_Helper_Plugin
{
    public partial class PluginMain : UserControl, IActPluginV1
    {
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\FFXIV_ACT_Helper_Plugin.config.xml");
        string bossDataFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Data\\FFXIV_ACT_Helper_Plugin.boss_data.xml");
        SettingsSerializer xmlSettings;

        string myName;
        List<MasterSwing> buffSwingHistory = new List<MasterSwing>();
        BossData bossData;
        List<MedicatedItem> medicatedItems = new List<MedicatedItem>();

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

        bool EnabledSimulateFFLogsDPSPerf
        {
            get
            {
                return this.checkBox3.Checked;
            }
        }

        bool EnabledCountOnlyTheLatestAndHighQuality
        {
            get
            {
                return this.checkBox4.Checked;
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
            pluginScreenSpace.Text = "FFXIV Helper Settings"; // Tab name
            this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();
            DownloadData();
            LoadData();
            UpdateUI();
            UpdateACTTables();

            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(BeforeLogLineRead);

            lblStatus.Text = "Plugin Started";
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
                            var item = this.medicatedItems.Where(x => x.Id == logComponents[9]).FirstOrDefault();

                            if (item != null)
                            {
                                string name = (this.myName == logComponents[6] ? ActGlobals.charName : logComponents[6]);

                                MasterSwing swing = new MasterSwing(21, false, Dnum.Unknown, DateTime.Parse(logComponents[1]), 0, logComponents[3], name, "", name);
                                swing.Tags.Add("Potency", 0);
                                //swing.Tags.Add("Job", "");
                                swing.Tags.Add("ActorID", logComponents[5]);
                                swing.Tags.Add("TargetID", logComponents[7]);
                                swing.Tags.Add("SkillID", item.SkillId);
                                swing.Tags.Add("BuffID", "49");
                                swing.Tags.Add("BuffDuration", double.Parse(logComponents[4]));
                                swing.Tags.Add("BuffByte1", item.BuffByte);
                                swing.Tags.Add("BuffByte2", "00");
                                swing.Tags.Add("BuffByte3", "00");

                                buffSwingHistory.Add(swing);
                            }
                        }
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
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void CheckedChanged(object sender, EventArgs e)
        {
            UpdateUI();
            UpdateACTTables();
        }

        void LoadSettings()
        {
            // Add any controls you want to save the state of
            xmlSettings.AddControlSetting(checkBox1.Name, checkBox1);
            xmlSettings.AddControlSetting(checkBox2.Name, checkBox2);
            xmlSettings.AddControlSetting(checkBox3.Name, checkBox3);
            xmlSettings.AddControlSetting(checkBox4.Name, checkBox4);

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

        void DownloadData()
        {
            // Download latest boss data
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.bossDataFile));
                new WebClient().DownloadFile("https://ugabugab.com/ffxiv-act-helper-plugin/boss_data.xml", this.bossDataFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        void LoadData()
        {
            // Load boss data
            try
            {
                FileStream fs = new FileStream(this.bossDataFile, FileMode.Open);
                XmlSerializer serializer = new XmlSerializer(typeof(BossData));
                this.bossData = (BossData)serializer.Deserialize(fs);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // Load medicated item data
            try
            {
                string[] rows = Properties.Resources.medicated_items
                    .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var row in rows)
                {
                    string[] cols = row.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length >= 3)
                    {
                        medicatedItems.Add(new MedicatedItem
                        {
                            Id = cols[0],
                            Name = cols[1],
                            SkillId = cols[2]
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        void UpdateUI()
        {
            checkBox4.Enabled = checkBox2.Checked;
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

            if (this.EnabledSimulateFFLogsDPSPerf)
            {
                if (!CombatantData.ColumnDefs.ContainsKey("Perf"))
                {
                    CombatantData.ColumnDefs.Add(
                        "Perf",
                        new CombatantData.ColumnDef(
                            "Perf",
                            true,
                            "INT",
                            "Perf",
                            new CombatantData.StringDataCallback(PerfDataCallback),
                            new CombatantData.StringDataCallback(PerfDataCallback),
                            new Comparison<CombatantData>(PerfSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("Perf"))
                {
                    CombatantData.ExportVariables.Add(
                        "Perf",
                        new CombatantData.TextExportFormatter(
                            "Perf",
                            "Perf",
                            "Simulated FFLogs DPS Perf",
                            new CombatantData.ExportStringDataCallback(PerfExporttDataCallback)));
                }
            }
            else
            {
                if (CombatantData.ColumnDefs.ContainsKey("Perf"))
                {
                    CombatantData.ColumnDefs.Remove("Perf");
                }
                if (CombatantData.ExportVariables.ContainsKey("Perf"))
                {
                    CombatantData.ExportVariables.Remove("Perf");
                }
            }

            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
        }

        string MedicatedCountDataCallback(CombatantData data)
        {
            var medicatedIncKeys = new string[] { "Medicated", "強化薬" }; // TODO: localize
            var medicatedBuffBytes = this.medicatedItems.Select(x => x.BuffByte).ToList();

            if (this.EnabledCountOnlyTheLatestAndHighQuality)
            {
                // Last 5 items are the latest & high quality
                medicatedBuffBytes = medicatedBuffBytes.Skip(medicatedBuffBytes.Count - 5).Take(5).ToList();
            }

            return data.AllInc
                .Where(x => medicatedIncKeys.Contains(x.Key))
                .Select(x => x.Value.Items
                    .Where(y => y.Tags.ContainsKey("BuffByte1") && medicatedBuffBytes.Contains(y.Tags["BuffByte1"]))
                    .Count())
                .Sum().ToString();
        }

        int MedicatedCountSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("MedicatedCount").CompareTo(right.GetColumnByName("MedicatedCount"));
        }

        string MedicatedCountExporttDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("MedicatedCount");
        }

        string PerfDataCallback(CombatantData data)
        {
            if (this.bossData != null)
            {
                var boss = this.bossData.Bosses
                    .Where(x => x.Zone == data.Parent.ZoneName)
                    .Where(x => data.Allies.ContainsKey(x.Name) || data.Allies.ContainsKey(x.NameJa))
                    .FirstOrDefault();

                if (boss != null)
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

                    var percentile = boss.Percentiles.Where(x => x.Job == job).FirstOrDefault();

                    if (percentile != null)
                    {
                        var dps = data.EncDPS;

                        // Add pet's DPS
                        var name = (data.Name == ActGlobals.charName) ? this.myName : data.Name;
                        dps += data.Parent.Items
                            .Where(x => x.Value.Name.Contains("(" + name + ")"))
                            .Select(x => x.Value.EncDPS)
                            .Sum();

                        // Recalculate DPS with applying exclusion period
                        var duration = data.Duration.TotalSeconds;
                        foreach (var exclusionPeriod in boss.ExclusionPeriods)
                        {
                            if (data.Duration.TotalSeconds > exclusionPeriod.StartTime)
                            {
                                duration -= (Math.Min(data.Duration.TotalSeconds, exclusionPeriod.EndTime) - exclusionPeriod.StartTime);
                            }
                        }
                        dps = (dps * data.Duration.TotalSeconds) / duration;

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

                        double p0 = 0.0;
                        double p1 = 100.0;
                        double d0 = 0.0;
                        double d1 = Double.MaxValue;

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

                        double perf = 1.0;
                        if (d0 != d1) // Avoid division by zero
                        {
                            perf = p0 + ((dps - d0) / ((d1 - d0) / (p1 - p0)));
                            perf = Math.Round(perf, MidpointRounding.AwayFromZero);
                            perf = Math.Max(perf, 1.0);
                        }

                        return ((int)perf).ToString();
                    }
                }
            }

            return "-";
        }

        int PerfSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("Perf").CompareTo(right.GetColumnByName("Perf"));
        }

        string PerfExporttDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("Perf");
        }
    }
}
