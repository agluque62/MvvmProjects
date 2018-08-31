using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Server;

using BkkSimV2.Model;

namespace BkkSimV2.Services
{
    public class BkkWebSocketService : WebSocketBehavior
    {
        WebSocketServer srv = new WebSocketServer();
        public BkkWebSocketService()
        {
            RefreshPeriod = 20;
            DataService = null;
            IsStarted = false;
        }

        #region Publicos
        public IDataService DataService { get; set; }
        public Action<BkkWebSocketService, bool> SessionRegister = null;

        public void RefreshUser(string nameofuser)
        {
            lock (locker)
            {
                DataService?.GetWorkingUsers((data, ex) =>
                {
                    var user = data.Users.Find(u => u.Name == nameofuser);
                    if (user != null)
                    {
                        /** Envio lo registrados */
                        Sessions?.Broadcast(MessageService.RegisteredMsg(user));
                        /** Envio los estados */
                        Sessions?.Broadcast(MessageService.StatusMsg(user));
                    }
                });
            }
        }

        #endregion Publicos

        #region Overrides
        /// <summary>
        /// Se ha conectado un cliente...
        /// </summary>
        /// <returns></returns>
        protected override Task OnOpen()
        {
            lock (locker)
            {
                SessionRegister?.Invoke(this, true);
                IsStarted = true;
                Task.Factory.StartNew(() =>
                {
                    DateTime lastRefresh = DateTime.MinValue;
                    while (IsStarted)
                    {
                        Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                        if ((DateTime.Now - lastRefresh) >= TimeSpan.FromSeconds(RefreshPeriod))
                        {
                            lastRefresh = DateTime.Now;
                            lock (locker)
                            {
                                DataService?.GetWorkingUsers((data, ex) =>
                                {
                                    /** Envio lo registrados */
                                    Send(MessageService.RegisteredMsg(data.Users));
                                    /** Envio los estados */
                                    Send(MessageService.StatusMsg(data.Users));
                                });
                            }
                        }
                    }
                });
            }
            return base.OnOpen();
        }

        protected override Task OnClose(CloseEventArgs e)
        {
            lock(locker)
            {
                SessionRegister?.Invoke(this, false);
                IsStarted = false;
            }
            return base.OnClose(e);
        }

        #endregion Overrides

        #region Internal Data and Methods

        private Int32 RefreshPeriod { get; set; }
        private bool IsStarted { get; set; }
        private object locker = new object();
        
        #endregion Internal Data
    }

    public class BkkWebSocketServer : WebSocketServer
    {
        public BkkWebSocketServer(IDataService dataService, string ip, int port) : 
            base (System.Net.IPAddress.Parse(ip), port)
        {
            AddWebSocketService<BkkWebSocketService>("/pbx/ws", () =>
            {
                return new BkkWebSocketService()
                {
                    DataService = dataService,
                    SessionRegister = (s, r) =>
                    {
                        if (r && sesiones.Contains(s) == false)
                            sesiones.Add(s);
                        else if (!r && sesiones.Contains(s) == true)
                            sesiones.Remove(s);
                    }
                };
            });
        }

        public void UpdateUser(string nameofuser)
        {
            sesiones.ForEach(s =>
            {
                s.RefreshUser(nameofuser);
            });
        }

        List<BkkWebSocketService> sesiones = new List<BkkWebSocketService>();
    }
}
