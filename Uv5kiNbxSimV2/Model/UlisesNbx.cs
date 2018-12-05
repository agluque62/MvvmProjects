using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using Newtonsoft.Json;
using NuMvvmServices;

namespace Uv5kiNbxSimV2.Model
{
    public enum  U5kServiceState { Slave = 0, Master = 1, Stopped = 2, NoIni = 3 };
    public enum NbxTypes { Radio, Telefonia, Ambos }
    public class UlisesNbxItem : IDisposable
    {
        public static event Action<string> NotifyChange;
        public static String ServerIp { get; set; }
        public static int ServerPort { get; set; }

        public IList<U5kServiceState> ServiceStates
        {
            get
            {
                var ret = Enum.GetValues(typeof(U5kServiceState)).Cast<U5kServiceState>().ToList<U5kServiceState>();
                return ret;
            }
        }

        public String Ip { get; set; }
        public int WebPort { get; set; }

        public U5kServiceState CfgService { get; set; }
        public U5kServiceState RadioService { get; set; }
        public U5kServiceState TifxService { get; set; }
        public U5kServiceState PresService { get; set; }
        /** */
        public U5kServiceState MnService { get; set; }
        public U5kServiceState PhoneService { get; set; }
        public NbxTypes NbxType { get; set; }

        public bool Active { get; set; }
        public bool Error { get; set; }

        public UlisesNbxItem()
        {
            CfgService = U5kServiceState.Stopped;
            RadioService = U5kServiceState.Stopped;
            TifxService = U5kServiceState.Stopped;
            PresService = U5kServiceState.Stopped;
            /** */
            MnService = U5kServiceState.Stopped;
            PhoneService = U5kServiceState.Stopped;
            NbxType = NbxTypes.Ambos;

        Active = false;

            Ip = "127.0.0.1";
            WebPort = 1022;
        }

        private readonly ILogService _log = new LogService();

        Task NbxJobTask = null;
        private bool Running { get; set; }
        public void Start()
        {
            if (NbxJobTask == null)
            {
                NbxJobTask = Task.Run(() =>
                {
                    IPEndPoint local = new IPEndPoint(IPAddress.Parse(Ip), 0);
                    IPEndPoint destino = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
                    try
                    {
                        Error = false;
                        using (UdpClient client = new UdpClient(local))
                        {
                            int count = 20;
                            Running = true;
                            while (Running)
                            {
                                Task.Delay(100).Wait();
                                if (count-- <= 0)
                                {
                                    count = 20;
                                    if (Active)
                                    {
                                        try
                                        {
#if _NBX_NOT_SPLITTED__
                                            Byte[] msg =
                                            {
                                        (Byte)CfgService,(Byte)RadioService,(Byte)TifxService,(Byte)PresService,
                                        (Byte)0xff,(Byte)0xff,(Byte)0xff,(Byte)0xff,
                                        (Byte)(WebPort & 0xff),
                                        (Byte)(WebPort >> 8)
                                    };
                                            client.Send(msg, msg.Length, destino);
                                            NotifyChange?.Invoke(String.Format("{0}: Sending msg C:{1}, R:{2}, T:{3}, P:{4}", Ip, msg[0], msg[1], msg[2], msg[3]));
#else

                                            if (NbxType== NbxTypes.Radio || NbxType== NbxTypes.Ambos)
                                            {
                                                var data = new
                                                {
                                                    Machine = Environment.MachineName,
                                                    ServerType = "RadioServer",
                                                    GlobalMaster = "Master",
                                                    RadioService = RadioService.ToString(),
                                                    CfgService = CfgService.ToString(),
                                                    PhoneService = PhoneService.ToString(),
                                                    TifxService = TifxService.ToString(),
                                                    PresenceService = PresService.ToString(),
                                                    WebPort,
                                                    TimeStamp = DateTime.Now
                                                };
                                                string msg = JsonConvert.SerializeObject(data);
                                                Byte[] bin = Encoding.ASCII.GetBytes(msg);
                                                client.Send(bin, bin.Length, destino);

                                                NotifyChange?.Invoke(String.Format("{0}: Sending msg {1}", Ip, msg));
                                            }

                                            if (NbxType == NbxTypes.Telefonia || NbxType == NbxTypes.Ambos)
                                            {
                                                var data = new
                                                {
                                                    Machine = Environment.MachineName,
                                                    ServerType = "PhoneServer",
                                                    GlobalMaster = "Master",
                                                    RadioService = RadioService.ToString(),
                                                    CfgService = CfgService.ToString(),
                                                    PhoneService = PhoneService.ToString(),
                                                    TifxService = TifxService.ToString(),
                                                    PresenceService = PresService.ToString(),
                                                    WebPort,
                                                    TimeStamp = DateTime.Now
                                                };
                                                string msg = JsonConvert.SerializeObject(data);
                                                Byte[] bin = Encoding.ASCII.GetBytes(msg);
                                                client.Send(bin, bin.Length, destino);

                                                NotifyChange?.Invoke(String.Format("{0}: Sending msg {1}", Ip, msg));
                                            }
#endif
                                        }
                                        catch (Exception x)
                                        {
                                            _log.Error($"UlisesNbx Exception", x);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception)
                    {
                        Error = true;
                    }
                });
            }
        }

        public void Dispose()
        {
            if (NbxJobTask != null)
            {
                Running = false;
                NbxJobTask.Wait(3000);
                NbxJobTask = null;
            }
        }

    }
}
