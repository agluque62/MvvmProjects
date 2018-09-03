using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using BkkSimV2.Model;

namespace BkkSimV2.Services
{
    class MessageService
    {
        public class BkkWsUserInfo
        {
            // Evento Register
            public string registered { get; set; }
            public string user { get; set; }
            // Evento Status,
            public long time { get; set; }
            public string other_number { get; set; }
            public string status { get; set; }

            public BkkWsUserInfo()
            {
                registered = "false";
                user = "";
                time = DateTime.Now.ToBinary();
                other_number = "";
                status = "-1";
            }
        }

        public class BkkWsMessage
        {
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public BkkWsUserInfo parametros { get; set; }

            public BkkWsMessage()
            {
                jsonrpc = "";
                method = "";
            }
        }

        public static string ServerStatusActive
        {
            get
            {
                BkkWsMessage msg = new BkkWsMessage()
                {
                    method = "notify_serverstatus",
                    parametros = new BkkWsUserInfo() { status = "active" }
                };
                return JsonConvert.SerializeObject(msg);
            }
        }
        /** */
        public static string ServerStatusClose
        {
            get
            {
                BkkWsMessage msg = new BkkWsMessage()
                {
                    method = "notify_serverstatus",
                    parametros = new BkkWsUserInfo() { status = "closed" }
                };
                return JsonConvert.SerializeObject(msg);
            }
        }
        /** */
        public static string RegisteredMsg(WorkingUser userInfo)
        {
            BkkWsMessage msg = new BkkWsMessage()
            {
                method = "notify_registered",
                parametros = new BkkWsUserInfo() { user = userInfo.Name, registered = userInfo.Status!= UserStatus.Unregistered ? "true" : "false" }
            };
            return JsonConvert.SerializeObject(msg);
        }
        /** */
        public static string RegisteredMsg(List<WorkingUser> userInfo)
        {
            List<BkkWsMessage> msgs = new List<BkkWsMessage>();
            userInfo.ForEach(user =>
            {
                msgs.Add(new BkkWsMessage()
                {
                    method = "notify_registered",
                    parametros = new BkkWsUserInfo() { user = user.Name, registered = user.Status != UserStatus.Unregistered ? "true" : "false" }
                });
            });
            return JsonConvert.SerializeObject(msgs);
        }
        /** */
        public static string StatusMsg(WorkingUser userInfo)
        {
            BkkWsMessage msg = new BkkWsMessage()
            {
                method = "notify_status",
                parametros = new BkkWsUserInfo() { user = userInfo.Name, status = userInfo.Status.ToString() }
            };
            return JsonConvert.SerializeObject(msg);
        }
        /** */
        public static string StatusMsg(List<WorkingUser> userInfo)
        {
            List<BkkWsMessage> msgs = new List<BkkWsMessage>();
            userInfo.ForEach(user =>
            {
                msgs.Add(new BkkWsMessage()
                {
                    method = "notify_status",
                    parametros = new BkkWsUserInfo() { user = user.Name, status = user.Status.ToString() }
                });
            });
            return JsonConvert.SerializeObject(msgs);
        }
    }
}
