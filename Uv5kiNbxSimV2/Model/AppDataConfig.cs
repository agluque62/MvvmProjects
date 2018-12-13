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
            public int RadioWp { get; set; }
            public int PhoneWp { get; set; }
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
                    ServerIP = "10.12.60.35",
                    ServerPort = 8103,
                    Nbxs = new ObservableCollection<JSonNbxConfig>()
                    {
                        new JSonNbxConfig(){ Ip="192.168.0.60", RadioWp=8101, PhoneWp=8102},
                        new JSonNbxConfig(){ Ip="192.168.0.61", RadioWp=8101, PhoneWp=8102},
                    }
                };
            }
            set
            {
            }
        }

    }
}
