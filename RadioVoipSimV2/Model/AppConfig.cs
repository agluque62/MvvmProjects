using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RadioVoipSimV2.Model
{
    public abstract class EquipmentConfig
    {
        public string Id { get; set; }
        public string Type { get; set; }
    }

    public class AppConfig
    {
        static string FileName = Properties.Settings.Default.ConfigFile; //  "config.json";

        public class FrequencyConfig
        {
            public string Id { get; set; }
            public string Band { get; set; }
            public ObservableCollection<MainEquipmentConfig> TxUsers { get; set; }
            public ObservableCollection<MainEquipmentConfig> RxUsers { get; set; }
        }

        public class MainEquipmentConfig : EquipmentConfig
        { 
            public int FrOff { get; set; }
            public int ChSp { get; set; }
            public int Pwr { get; set; }
            public int Mod { get; set; }
        }

        public class StandbyEquipmentConfig : EquipmentConfig
        {
            public string Band { get; set; }
        }

        public class SnmpConfig
        {
            public string AgentIp { get; set; }
            public int AgentPort { get; set; }
            public string BaseOid { get; set; }
            public string QueryOid { get; set; }
            public string AnswerOid { get; set; }
        }

        private string _voipAgentIP;
        private int _voipAgentPort;
        private int _pttOn2SqhOn;
        private int _pttOff2SqhOff;
        private ObservableCollection<FrequencyConfig> _simulatedFrequencies;
        private SnmpConfig _snmp;
        private ObservableCollection<StandbyEquipmentConfig> _standbyEquipments;

        public string VoipAgentIP { get => _voipAgentIP; set => _voipAgentIP = value; }
        public int VoipAgentPort { get => _voipAgentPort; set => _voipAgentPort = value; }
        public int PttOn2SqhOn { get => _pttOn2SqhOn; set => _pttOn2SqhOn = value; }
        public int PttOff2SqhOff { get => _pttOff2SqhOff; set => _pttOff2SqhOff = value; }
        public ObservableCollection<FrequencyConfig> SimulatedFrequencies { get => _simulatedFrequencies; set => _simulatedFrequencies = value; }
        public SnmpConfig Snmp { get => _snmp; set => _snmp = value; }
        public ObservableCollection<StandbyEquipmentConfig> StandbyEquipments { get => _standbyEquipments; set => _standbyEquipments = value; }

        public static void GetAppConfig(Action<AppConfig, Exception> callback)
        {
            bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (designTime)
            {
                var cfg = new AppConfig()
                {
                    VoipAgentIP = "127.0.0.1",
                    VoipAgentPort = 7060,
                    SimulatedFrequencies = new ObservableCollection<FrequencyConfig>(),
                    PttOn2SqhOn = 50,
                    PttOff2SqhOff = 50,
                };
                cfg.SimulatedFrequencies.Add(new FrequencyConfig()
                {
                    Id = "199.000",
                    TxUsers = { new MainEquipmentConfig() { Id = "TX001" } },
                    RxUsers = { new MainEquipmentConfig() { Id = "RX001" } }
                });
                callback(cfg, null);
            }
            else
            {
                AppConfig appConfig = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(FileName));
                foreach (var f in appConfig.SimulatedFrequencies)
                {
                    foreach (var t in f.TxUsers)
                        t.Type = "Tx";
                    foreach (var r in f.RxUsers)
                        r.Type = "Rx";
                }
                callback(appConfig, null);
            }
        }
        public static void SetAppConfig(AppConfig cfg)
        {
            bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (designTime)
            {
            }
            else
            {
                File.WriteAllText(FileName, JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
        }


        public List<MainEquipmentConfig> EquipmentsInFreq(FrequencyConfig f)
        {
            List<MainEquipmentConfig> equipments = new List<MainEquipmentConfig>();

            equipments.AddRange(new List<MainEquipmentConfig>((IEnumerable<MainEquipmentConfig>)f.TxUsers));
            equipments.AddRange(new List<MainEquipmentConfig>((IEnumerable<MainEquipmentConfig>)f.RxUsers));

            return equipments;
        }

        public int EquipmentsCount
        {
            get
            {
                int count = 0;
                foreach (var f in _simulatedFrequencies)
                {
                    foreach (var t in f.TxUsers)
                        count++;
                    foreach (var r in f.RxUsers)
                        count++;
                }
                return count;
            }
        }
    }
}
