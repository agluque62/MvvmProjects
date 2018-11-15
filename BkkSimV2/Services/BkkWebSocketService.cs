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
        public void RefreshUser(string nameofuser, bool infoRegRefresh, bool infoStatusRefresh, bool infoReg=true)
        {
            lock (locker)
            {
                DataService?.GetWorkingUsers((data, ex) =>
                {
                    var user = data.Users.Find(u => u.Name == nameofuser);
                    if (user != null)
                    {
                        /** Envio lo registrados */
                        if (infoRegRefresh==true)
                            Sessions?.Broadcast(MessageService.RegisteredMsg(user, infoReg));
                        /** Envio los estados */
                        if (infoStatusRefresh==true)
                            Sessions?.Broadcast(MessageService.StatusMsg(user));
                    }
                });
            }
        }

        public void DisposeSessions()
        {
            lock (locker)
            {
                Sessions.CloseSession(this.Id, CloseStatusCode.Normal, "Server shutdowm");
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
                    /** */
                    Send(MessageService.ServerStatusActive);

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
                                    if (ex == null)
                                    {
                                        /** Envio lo registrados */
                                        Send(MessageService.RegisteredMsg(data.Users));
                                        /** Envio los estados */
                                        Send(MessageService.StatusMsg(data.Users));
                                    }
                                });
                            }
                        }
                    }
                    Send(MessageService.ServerStatusClose);
                });
                BkkMessaging.Send(ModelEvents.SessionOpen, null);
            }
            return base.OnOpen();
        }

        protected override Task OnClose(CloseEventArgs e)
        {
            lock(locker)
            {
                SessionRegister?.Invoke(this, false);
                IsStarted = false;
                BkkMessaging.Send(ModelEvents.SessionClose, null);
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
        const string strService = "/pbx/ws";
        public BkkWebSocketServer(string ip, int port) : 
            base (System.Net.IPAddress.Parse(ip), port)
        {
        }

        public void Activate(IDataService dataService)
        {
            AddWebSocketService<BkkWebSocketService>(strService, () =>
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

        public void Deactivate()
        {
            var copia = new List<BkkWebSocketService>(sesiones);
            copia.ForEach(sesion =>
            {
                sesion.DisposeSessions();
            });

            sesiones.Clear();
        }

        public void UpdateUser(string nameofuser, bool infoRegRefresh, bool infoStatusRefresh)
        {
            sesiones.ForEach(s =>
            {
                s.RefreshUser(nameofuser, infoRegRefresh, infoStatusRefresh);
            });
        }

        public void InformUserUnregistered(string nameofuser)
        {
            sesiones.ForEach(s =>
            {
                s.RefreshUser(nameofuser, true, false, false);
            });

        }

        List<BkkWebSocketService> sesiones = new List<BkkWebSocketService>();
    }
}
