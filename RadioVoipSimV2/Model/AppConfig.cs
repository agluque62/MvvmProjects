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
            public List<string> TxUsers { get; set; }
            public List<string> RxUsers { get; set; }
        }

        private string _voipAgentIP;
        private int _voipAgentPort;
        private ObservableCollection<FrequencyConfig> _simulatedFrequencies;

        public string VoipAgentIP { get => _voipAgentIP; set => _voipAgentIP = value; }
        public int VoipAgentPort { get => _voipAgentPort; set => _voipAgentPort = value; }
        public ObservableCollection<FrequencyConfig> SimulatedFrequencies { get => _simulatedFrequencies; set => _simulatedFrequencies = value; }

        public static void GetAppConfig(Action<AppConfig, Exception> callback)
        {
            bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
            if (designTime)
            {
                var cfg = new AppConfig()
                {
                    VoipAgentIP = "127.0.0.1",
                    VoipAgentPort = 7060,
                    SimulatedFrequencies = new ObservableCollection<FrequencyConfig>()
                };
                cfg.SimulatedFrequencies.Add(new FrequencyConfig()
                {
                    Id="199.000",
                    TxUsers = {"TX001"},
                    RxUsers = {"RX001"}
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
