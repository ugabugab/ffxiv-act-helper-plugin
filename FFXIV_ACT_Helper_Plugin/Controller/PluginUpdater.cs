using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public class PluginUpdater
    {
        string temporaryPluignFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "FFXIV_ACT_Helper_Plugin.zip");

        IActPluginV1 plugin;
        Thread updateThread;

        public PluginUpdater(IActPluginV1 plugin)
        {
            this.plugin = plugin;
        }

        public void UpdateCheckOnBackground()
        {
            updateThread = new Thread(new ThreadStart(this.UpdateCheck))
            {
                IsBackground = true
            };
            updateThread.Start();
        }

        public void UpdateCheck()
        {
            try
            {
                // Check new version
                Stream stream;
                if (PluginDebug.UsesDebugData)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    stream = assembly.GetManifestResourceStream("FFXIV_ACT_Helper_Plugin.Resources.DebugPluginData.xml");
                }
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        var data = Encoding.Default.GetString(client.DownloadData("https://ugabugab.github.io/ffxiv-act-helper-plugin/data/plugin_data.xml"));
                        stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                    }
                }
                XmlSerializer serializer = new XmlSerializer(typeof(PluginData));
                var pluginData = (PluginData)serializer.Deserialize(stream);

                Version currentVersion = typeof(PluginMain).Assembly.GetName().Version;
                Version latestVersion = new Version(pluginData.Version);

                if (currentVersion < latestVersion)
                {
                    // Show update confirming message
                    ActGlobalsExtension.RunOnACTUIThread(delegate
                    {
                        TraySlider traySlider = new TraySlider
                        {
                            ButtonLayout = TraySlider.ButtonLayoutEnum.TwoButton,
                            ButtonSW = { Text = "Update" },
                            ButtonSE = { Text = "Cancel" },
                            TrayTitle = { Text = "Plugin Update" },
                            TrayText = { Text = Properties.Resources.MessagePluginUpdate },
                            ShowDurationMs = 30000
                        };
                        traySlider.ButtonSW.Click += delegate (object sender, EventArgs eventArgs)
                        {
                            UpdatePlugin(pluginData.Url);
                        };
                        traySlider.ShowTraySlider();
                    });
                }
            }
            catch (ThreadAbortException)
            {
                // Do nothing
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        void UpdatePlugin(string url)
        {
            try
            {
                // Download new version
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(url, temporaryPluignFile);
                }
                FileInfo fileInfo = new FileInfo(temporaryPluignFile);

                // Replace plguin file to new version
                ActPluginData actPluginData = ActGlobals.oFormActMain.PluginGetSelfData(plugin);
                actPluginData.pluginFile.Delete();
                ActGlobals.oFormActMain.UnZip(fileInfo.FullName, actPluginData.pluginFile.DirectoryName);

                // Restart ACT
                ActGlobals.oFormActMain.RestartACT(true, Properties.Resources.MessagePluginUpdateCompleted);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        void ShowErrorMessage(Exception ex)
        {
            ActGlobalsExtension.RunOnACTUIThread(delegate
            {
                new TraySlider
                {
                    ButtonLayout = TraySlider.ButtonLayoutEnum.OneButton,
                    TrayTitle = { Text = "Plugin Update Error" },
                    TrayText = { Text = string.Format(Properties.Resources.MessagePluginUpdateError, ex.Message) }
                }
                .ShowTraySlider();
            });
        }
    }
}
