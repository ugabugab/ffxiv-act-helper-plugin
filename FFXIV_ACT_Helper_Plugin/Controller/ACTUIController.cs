using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    class ACTUIController
    {
        public void UpdateTable()
        {
            if (PluginMain.Shared.EnabledCountMedicatedBuffs)
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
                            new CombatantData.StringDataCallback(MedicatedCountCellDataCallback),
                            new CombatantData.StringDataCallback(MedicatedCountSqlDataCallback),
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
                            new CombatantData.ExportStringDataCallback(MedicatedCountExportDataCallback)));
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

            if (PluginMain.Shared.EnabledSimulateFFLogsDPSPerf)
            {
                // rPerf
                if (!CombatantData.ColumnDefs.ContainsKey("rPerf"))
                {
                    CombatantData.ColumnDefs.Add(
                        "rPerf",
                        new CombatantData.ColumnDef(
                            "rPerf",
                            true,
                            "INT",
                            "rPerf",
                            new CombatantData.StringDataCallback(RPerfCellDataCallback),
                            new CombatantData.StringDataCallback(RPerfSqlDataCallback),
                            new Comparison<CombatantData>(RPerfSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("rPerf"))
                {
                    CombatantData.ExportVariables.Add(
                        "rPerf",
                        new CombatantData.TextExportFormatter(
                            "rPerf",
                            "rPerf",
                            "Simulated FFLogs rDPS Perf",
                            new CombatantData.ExportStringDataCallback(RPerfExportDataCallback)));
                }
                // aPerf
                if (!CombatantData.ColumnDefs.ContainsKey("aPerf"))
                {
                    CombatantData.ColumnDefs.Add(
                        "aPerf",
                        new CombatantData.ColumnDef(
                            "aPerf",
                            false,
                            "INT",
                            "aPerf",
                            new CombatantData.StringDataCallback(APerfCellDataCallback),
                            new CombatantData.StringDataCallback(APerfSqlDataCallback),
                            new Comparison<CombatantData>(APerfSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("aPerf"))
                {
                    CombatantData.ExportVariables.Add(
                        "aPerf",
                        new CombatantData.TextExportFormatter(
                            "aPerf",
                            "aPerf",
                            "Simulated FFLogs aDPS Perf",
                            new CombatantData.ExportStringDataCallback(APerfExportDataCallback)));
                }
                // rDPS
                if (!CombatantData.ColumnDefs.ContainsKey("rDPS"))
                {
                    CombatantData.ColumnDefs.Add(
                        "rDPS",
                        new CombatantData.ColumnDef(
                            "rDPS",
                            true,
                            "DOUBLE",
                            "rDPS",
                            new CombatantData.StringDataCallback(RDPSCellDataCallback),
                            new CombatantData.StringDataCallback(RDPSSqlDataCallback),
                            new Comparison<CombatantData>(RDPSSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("rdps"))
                {
                    CombatantData.ExportVariables.Add(
                        "rdps",
                        new CombatantData.TextExportFormatter(
                            "rdps",
                            "rdps",
                            "Simulated FFLogs rDPS",
                            new CombatantData.ExportStringDataCallback(RDPSAsDoubleExportDataCallback)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("RDPS"))
                {
                    CombatantData.ExportVariables.Add(
                        "RDPS",
                        new CombatantData.TextExportFormatter(
                            "RDPS",
                            "RDPS",
                            "Simulated FFLogs rDPS",
                            new CombatantData.ExportStringDataCallback(RDPSAsIntExportDataCallback)));
                }
                // aDPS
                if (!CombatantData.ColumnDefs.ContainsKey("aDPS"))
                {
                    CombatantData.ColumnDefs.Add(
                        "aDPS",
                        new CombatantData.ColumnDef(
                            "aDPS",
                            false,
                            "DOUBLE",
                            "aDPS",
                            new CombatantData.StringDataCallback(ADPSCellDataCallback),
                            new CombatantData.StringDataCallback(ADPSSqlDataCallback),
                            new Comparison<CombatantData>(ADPSSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("adps"))
                {
                    CombatantData.ExportVariables.Add(
                        "adps",
                        new CombatantData.TextExportFormatter(
                            "adps",
                            "adps",
                            "Simulated FFLogs aDPS",
                            new CombatantData.ExportStringDataCallback(ADPSAsDoubleExportDataCallback)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("ADPS"))
                {
                    CombatantData.ExportVariables.Add(
                        "ADPS",
                        new CombatantData.TextExportFormatter(
                            "ADPS",
                            "ADPS",
                            "Simulated FFLogs aDPS",
                            new CombatantData.ExportStringDataCallback(ADPSAsIntExportDataCallback)));
                }
                // rDPSPortions
                if (!CombatantData.ColumnDefs.ContainsKey("rDPSPortions"))
                {
                    CombatantData.ColumnDefs.Add(
                        "rDPSPortions",
                        new CombatantData.ColumnDef(
                            "rDPSPortions",
                            false,
                            "TEXT",
                            "rDPSPortions",
                            new CombatantData.StringDataCallback(RDPSPortionsDataCallback),
                            new CombatantData.StringDataCallback(RDPSPortionsDataCallback),
                            new Comparison<CombatantData>(RDPSPortionsSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("rDPSPortions"))
                {
                    CombatantData.ExportVariables.Add(
                        "rDPSPortions",
                        new CombatantData.TextExportFormatter(
                            "rDPSPortions",
                            "rDPSPortions",
                            "Simulated FFLogs rDPS portions",
                            new CombatantData.ExportStringDataCallback(RDPSPortionsExportDataCallback)));
                }
                // aDPSPortions
                if (!CombatantData.ColumnDefs.ContainsKey("aDPSPortions"))
                {
                    CombatantData.ColumnDefs.Add(
                        "aDPSPortions",
                        new CombatantData.ColumnDef(
                            "aDPSPortions",
                            false,
                            "TEXT",
                            "aDPSPortions",
                            new CombatantData.StringDataCallback(ADPSPortionsDataCallback),
                            new CombatantData.StringDataCallback(ADPSPortionsDataCallback),
                            new Comparison<CombatantData>(ADPSPortionsSortComparer)));
                }
                if (!CombatantData.ExportVariables.ContainsKey("aDPSPortions"))
                {
                    CombatantData.ExportVariables.Add(
                        "aDPSPortions",
                        new CombatantData.TextExportFormatter(
                            "aDPSPortions",
                            "aDPSPortions",
                            "Simulated FFLogs aDPS portions",
                            new CombatantData.ExportStringDataCallback(ADPSPortionsExportDataCallback)));
                }
            }
            else
            {
                // rPerf
                if (CombatantData.ColumnDefs.ContainsKey("rPerf"))
                {
                    CombatantData.ColumnDefs.Remove("rPerf");
                }
                if (CombatantData.ExportVariables.ContainsKey("rPerf"))
                {
                    CombatantData.ExportVariables.Remove("rPerf");
                }
                // aPerf
                if (CombatantData.ColumnDefs.ContainsKey("aPerf"))
                {
                    CombatantData.ColumnDefs.Remove("aPerf");
                }
                if (CombatantData.ExportVariables.ContainsKey("aPerf"))
                {
                    CombatantData.ExportVariables.Remove("aPerf");
                }
                // rDPS
                if (CombatantData.ColumnDefs.ContainsKey("rDPS"))
                {
                    CombatantData.ColumnDefs.Remove("rDPS");
                }
                if (CombatantData.ExportVariables.ContainsKey("rdps"))
                {
                    CombatantData.ExportVariables.Remove("rdps");
                }
                if (CombatantData.ExportVariables.ContainsKey("RDPS"))
                {
                    CombatantData.ExportVariables.Remove("RDPS");
                }
                // aDPS
                if (CombatantData.ColumnDefs.ContainsKey("aDPS"))
                {
                    CombatantData.ColumnDefs.Remove("aDPS");
                }
                if (CombatantData.ExportVariables.ContainsKey("adps"))
                {
                    CombatantData.ExportVariables.Remove("adps");
                }
                if (CombatantData.ExportVariables.ContainsKey("ADPS"))
                {
                    CombatantData.ExportVariables.Remove("ADPS");
                }
                // rDPSPortions
                if (CombatantData.ColumnDefs.ContainsKey("rDPSPortions"))
                {
                    CombatantData.ColumnDefs.Remove("rDPSPortions");
                }
                if (CombatantData.ExportVariables.ContainsKey("rDPSPortions"))
                {
                    CombatantData.ExportVariables.Remove("rDPSPortions");
                }
                // aDPSPortions
                if (CombatantData.ColumnDefs.ContainsKey("aDPSPortions"))
                {
                    CombatantData.ColumnDefs.Remove("aDPSPortions");
                }
                if (CombatantData.ExportVariables.ContainsKey("aDPSPortions"))
                {
                    CombatantData.ExportVariables.Remove("aDPSPortions");
                }
            }

            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
        }

        // MedicatedCount
        string MedicatedCountCellDataCallback(CombatantData data)
        {
            return data.GetMedicatedCount(PluginMain.Shared.EnabledCountOnlyTheLatestAndHighQuality).ToString();
        }

        string MedicatedCountSqlDataCallback(CombatantData data)
        {
            return data.GetMedicatedCount(PluginMain.Shared.EnabledCountOnlyTheLatestAndHighQuality).ToString();
        }

        int MedicatedCountSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("MedicatedCount").CompareAsIntTo(right.GetColumnByName("MedicatedCount"));
        }

        string MedicatedCountExportDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("MedicatedCount");
        }

        // aPerf
        string APerfCellDataCallback(CombatantData data)
        {
            var value = data.GetAPerf();
            return value > 0 ? value.ToString() : "-";
        }

        string APerfSqlDataCallback(CombatantData data)
        {
            var value = data.GetAPerf();
            return value > 0 ? value.ToString() : "0";
        }

        int APerfSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("aPerf").CompareAsIntTo(right.GetColumnByName("aPerf"));
        }

        string APerfExportDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("aPerf");
        }

        // rPerf
        string RPerfCellDataCallback(CombatantData data)
        {
            var value = data.GetRPerf();
            return value > 0 ? value.ToString() : "-";
        }

        string RPerfSqlDataCallback(CombatantData data)
        {
            var value = data.GetRPerf();
            return value > 0 ? value.ToString() : "0";
        }

        int RPerfSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("rPerf").CompareAsIntTo(right.GetColumnByName("rPerf"));
        }

        string RPerfExportDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("rPerf");
        }

        // aDPS
        string ADPSCellDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null 
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "-";

            var value = data.GetADPS();
            return value > 0 ? value.ToString("#,0.00") : "-";
        }

        string ADPSSqlDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "0.0";

            var value = data.GetADPS();
            return value > 0 ? value.ToString() : "0.0";
        }

        int ADPSSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("aDPS").CompareAsDoubleTo(right.GetColumnByName("aDPS"));
        }

        string ADPSAsDoubleExportDataCallback(CombatantData data, string extraFormat)
        {
            if (double.TryParse(data.GetColumnByName("aDPS"), out double value) == false)
            {
                return "-";
            }
            return value.ToString();
        }

        string ADPSAsIntExportDataCallback(CombatantData data, string extraFormat)
        {
            if (double.TryParse(data.GetColumnByName("aDPS"), out double value) == false)
            {
                return "-";
            }
            return value.ToString("#0");
        }

        // rDPS
        string RDPSCellDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "-";

            var value = data.GetRDPS();
            return value > 0 ? value.ToString("#,0.00") : "-";
        }

        string RDPSSqlDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "0.0";

            var value = data.GetRDPS();
            return value > 0 ? value.ToString() : "0.0";
        }

        int RDPSSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("rDPS").CompareAsDoubleTo(right.GetColumnByName("rDPS"));
        }

        string RDPSAsDoubleExportDataCallback(CombatantData data, string extraFormat)
        {
            if (double.TryParse(data.GetColumnByName("rDPS"), out double value) == false)
            {
                return "-";
            }
            return value.ToString();
        }

        string RDPSAsIntExportDataCallback(CombatantData data, string extraFormat)
        {
            if (double.TryParse(data.GetColumnByName("rDPS"), out double value) == false)
            {
                return "-";
            }
            return value.ToString("#0");
        }

        // rDPSPortions
        string RDPSPortionsDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "";

            var givenDPSGroup = data.GetRGivenDPSGroup();
            var takenDPSGroup = data.GetRTakenDPSGroup();

            string value = "";
            if (takenDPSGroup != null && givenDPSGroup != null)
            {
                foreach (KeyValuePair<string, double> kv in givenDPSGroup.OrderByDescending(x => Math.Abs(x.Value)))
                {
                    if (value.Length > 0)
                    {
                        value += " | ";
                    }
                    value += kv.Key + " : " + "+" + (kv.Value).ToString("#,0.00");
                }
                foreach (KeyValuePair<string, double> kv in takenDPSGroup.OrderByDescending(x => Math.Abs(x.Value)))
                {
                    if (value.Length > 0)
                    {
                        value += " | ";
                    }
                    value += kv.Key + " : " + "-" + (kv.Value).ToString("#,0.00");
                }
            }

            return value;
        }

        int RDPSPortionsSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("rDPSPortions").CompareAsDoubleTo(right.GetColumnByName("rDPSPortions"));
        }

        string RDPSPortionsExportDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("rDPSPortions");
        }

        // aDPSPortions
        string ADPSPortionsDataCallback(CombatantData data)
        {
            if (data.Parent.GetBoss() == null
                && !PluginMain.Shared.EnabledCalculateRDPSADPSForALlZones) return "";

            var takenDPSGroup = data.GetATakenDPSGroup();

            string value = "";
            if (takenDPSGroup != null && takenDPSGroup != null)
            {
                foreach (KeyValuePair<string, double> kv in takenDPSGroup.OrderByDescending(x => Math.Abs(x.Value)))
                {
                    if (value.Length > 0)
                    {
                        value += " | ";
                    }
                    value += kv.Key + " : " + "-" + (kv.Value).ToString("#,0.00");
                }
            }

            return value;
        }

        int ADPSPortionsSortComparer(CombatantData left, CombatantData right)
        {
            return left.GetColumnByName("aDPSPortions").CompareAsDoubleTo(right.GetColumnByName("aDPSPortions"));
        }

        string ADPSPortionsExportDataCallback(CombatantData data, string extraFormat)
        {
            return data.GetColumnByName("aDPSPortions");
        }
    }
}
