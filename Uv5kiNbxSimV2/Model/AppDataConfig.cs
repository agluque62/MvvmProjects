using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Uv5kiNbxSimV2.Model
{
    public class AppDataConfig
    {
        const string FileName = "config.json";
        public class JSonNbxConfig
        {
            public string Ip { get; set; }
            public int Wp { get; set; }
            public JSonNbxConfig()
            {
                Hash = DateTime.Now.Ticks;
            }

            private Int64 Hash { get; set; }
        }
        public class JSonConfig
        {
            public string ServerIP { get; set; }
            public int ServerPort { get; set; }
            public ObservableCollection<JSonNbxConfig> Nbxs { get; set; }
        }

        public JSonConfig Config
        {
            get
            {
                return JsonConvert.DeserializeObject<JSonConfig>(File.ReadAllText(FileName));
            }
            set
            {
                File.WriteAllText(FileName, JsonConvert.SerializeObject(value, Formatting.Indented));
            }
        }

        public JSonConfig DesignConfig
        {
            get
            {
                return new JSonConfig()
                {
                    ServerIP = "192.168.0.212",
                    ServerPort = 1024,
                    Nbxs = new ObservableCollection<JSonNbxConfig>()
                    {
                        new JSonNbxConfig(){ Ip="192.168.0.60", Wp=8000},
                        new JSonNbxConfig(){ Ip="192.168.0.61", Wp=8001},
                    }
                };
            }
            set
            {
            }
        }

    }
}
