﻿using System;
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
using System.Reflection;

namespace FFXIV_ACT_Helper_Plugin
{
    public partial class PluginMain : UserControl, IActPluginV1
    {
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\FFXIV_ACT_Helper_Plugin.config.xml");
        string bossDataFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Data\\FFXIV_ACT_Helper_Plugin.boss_data.xml");

        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        SettingsSerializer xmlSettings;
        ACTEventHandler actEventHandler = new ACTEventHandler();
        ACTUIController actUIController = new ACTUIController();

        public static PluginMain Shared { get; private set; } = null;

        public bool EnabledEndCombatWhenRestartContent => this.checkBox1.Checked;

        public bool EnabledDetectBuffsDuringNonCombat => this.checkBox2.Checked;

        public bool EnabledCountMedicatedBuffs => this.checkBox2.Checked;

        public bool EnabledSimulateFFLogsParses => this.checkBox3.Checked;

        public bool EnabledCountOnlyTheLatestAndHighQuality => this.checkBox4.Checked;

        public bool EnabledCalculateRDPSADPSForALlZones => this.checkBox5.Checked;

        public PluginMain()
        {
            InitializeComponent();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            Shared = this;

            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
            pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
            pluginScreenSpace.Text = "FFXIV Helper Settings"; // Tab name
            this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance

            LoadSettings();
            DownloadData();
            LoadData();
            UpdateUI();
            actUIController.UpdateTable();
            actEventHandler.Setup();

            // Automatic update
            if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())
            {
                new PluginUpdater(this).UpdateCheckOnBackground();
            }

            lblStatus.Text = Properties.Resources.MessagePluginInit;
        }

        public void DeInitPlugin()
        {
            actEventHandler.Teardown();
            SaveSettings();

            lblStatus.Text = Properties.Resources.MessagePluginDeinit;

            Shared = null;
        }

        public void UpdateCheck()
        {
            new PluginUpdater(this).UpdateCheck();
        }

        void LoadSettings()
        {
            // Add any controls you want to save the state of
            xmlSettings.AddControlSetting(checkBox1.Name, checkBox1);
            xmlSettings.AddControlSetting(checkBox2.Name, checkBox2);
            xmlSettings.AddControlSetting(checkBox3.Name, checkBox3);
            xmlSettings.AddControlSetting(checkBox4.Name, checkBox4);
            xmlSettings.AddControlSetting(checkBox5.Name, checkBox5);

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
            XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 1,
                IndentChar = '\t'
            };
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
                new WebClient().DownloadFile("https://ugabugab.github.io/ffxiv-act-helper-plugin/data/boss_data.xml", this.bossDataFile);
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
                Stream fs;
                if (PluginDebug.UsesDebugData)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    fs = assembly.GetManifestResourceStream("FFXIV_ACT_Helper_Plugin.Resources.DebugBossData.xml");
                }
                else
                {
                    fs = new FileStream(this.bossDataFile, FileMode.Open);
                }
                XmlSerializer serializer = new XmlSerializer(typeof(BossData));
                var dossData = (BossData)serializer.Deserialize(fs);
                ActGlobalsExtension.Bosses = dossData.Bosses;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // Load buff data
            try
            {
                string[] rows = Properties.Resources.Buffs
                    .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var items = new List<Buff>();
                foreach (var row in rows)
                {
                    string[] cols = row.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    items.Add(new Buff
                    {
                        Id = cols[0],
                        Name = cols[1],
                        NameJa = cols[2],
                        Type = (BuffType)int.Parse(cols[3]),
                        Target = (BuffTarget)int.Parse(cols[4]),
                        Duration = int.Parse(cols[5]),
                        DamageRate = int.Parse(cols[6]),
                        CriticalRate = int.Parse(cols[7]),
                        DirectHitRate = int.Parse(cols[8]),
                        Group = (BuffGroup)int.Parse(cols[9]),
                        IsContinuous = int.Parse(cols[10]) != 0
                    });
                }
                ActGlobalsExtension.Buffs = items;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // Load skill data
            try
            {
                string[] rows = Properties.Resources.Skills
                    .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var items = new List<Skill>();
                foreach (var row in rows)
                {
                    string[] cols = row.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    items.Add(new Skill
                    {
                        Id = cols[0],
                        Name = cols[1],
                        NameJa = cols[2],
                        CriticalRate = int.Parse(cols[3]),
                        DirectHitRate = int.Parse(cols[4]),
                    });
                }
                ActGlobalsExtension.Skills = items;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // Load medicated item data
            try
            {
                string[] rows = Properties.Resources.MedicatedItems
                    .Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var items = new List<MedicatedItem>();
                foreach (var row in rows)
                {
                    string[] cols = row.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    if (cols.Length >= 3)
                    {
                        items.Add(new MedicatedItem
                        {
                            Id = cols[0],
                            Name = cols[1],
                            SkillId = cols[2]
                        });
                    }
                }
                ActGlobalsExtension.MedicatedItems = items;
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

        private void OnCheckedChange(object sender, EventArgs e)
        {
            UpdateUI();
            actUIController.UpdateTable();
        }

        private void OnMouseHover(object sender, EventArgs e)
        {
            if (sender == checkBox1)
            {
                label1.Text = Properties.Resources.HelpEndCombatWhenRestartContent;
            }
            else if (sender == checkBox2)
            {
                label1.Text = Properties.Resources.HelpCountMedicatedBuffs;
            }
            else if (sender == checkBox3)
            {
                label1.Text = String.Format(Properties.Resources.HelpSimulateFFlogsParses, 
                    string.Join(Environment.NewLine, ActGlobalsExtension.Bosses.Select(x => "- " + x.Zone).Distinct()));
            }
            else if (sender == checkBox4)
            {
                label1.Text = Properties.Resources.HelpCountOnlyTheLatestAndHighQuality;
            }
            else if (sender == checkBox5)
            {
                label1.Text = Properties.Resources.HelpCalculateFFLogsDPSHPSForAllZones;
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (sender == checkBox1
                || sender == checkBox2
                || sender == checkBox3
                || sender == checkBox4
                || sender == checkBox5)
            {
                label1.Text = Properties.Resources.HelpEmpty;
            }
        }
    }
}
