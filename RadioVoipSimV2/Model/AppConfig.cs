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
    public class AppConfig
    {
        const string FileName = "config.json";


        public class FrequencyConfig
        {
            public string Id { get; set; }
            public ObservableCollection<EquipmentConfig> TxUsers { get; set; }
            public ObservableCollection<EquipmentConfig> RxUsers { get; set; }
        }

        public class EquipmentConfig
        {
            public string Id { get; set; }
        }

        private string _voipAgentIP;
        private int _voipAgentPort;
        private ObservableCollection<FrequencyConfig> _simulatedFrequencies;
        private int _pttOn2SqhOn;
        private int _pttOff2SqhOff;

        public string VoipAgentIP { get => _voipAgentIP; set => _voipAgentIP = value; }
        public int VoipAgentPort { get => _voipAgentPort; set => _voipAgentPort = value; }
        public ObservableCollection<FrequencyConfig> SimulatedFrequencies { get => _simulatedFrequencies; set => _simulatedFrequencies = value; }
        public int PttOn2SqhOn { get => _pttOn2SqhOn; set => _pttOn2SqhOn = value; }
        public int PttOff2SqhOff { get => _pttOff2SqhOff; set => _pttOff2SqhOff = value; }

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
                    TxUsers = { new EquipmentConfig() { Id = "TX001" } },
                    RxUsers = { new EquipmentConfig() { Id = "RX001" } }
                });
                callback(cfg, null);
            }
            else
            {
                callback(JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(FileName)), null);
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
    }
}
