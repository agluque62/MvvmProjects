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
    class BkkWebSocketService : WebSocketBehavior
    {
        WebSocketServer srv = new WebSocketServer();
        public BkkWebSocketService()
        {
            RefreshPeriod = 60;
            DataService = null;
            IsStarted = false;
        }

        #region Publicos
        public IDataService DataService { get; set; }

        public void Start()
        {
            lock (locker)
            {
                if (!IsStarted)
                {
                    IsStarted = true;

                    Task.Factory.StartNew(() =>
                    {
                        DateTime lastRefresh = DateTime.Now;
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
                                        Sessions.Broadcast(MessageService.RegisteredMsg(data.Users));
                                    /** Envio los estados */
                                        Sessions.Broadcast(MessageService.StatusMsg(data.Users));
                                    });
                                }
                            }
                        }
                    });
                }
            }
        }

        public void Stop()
        {
            lock (locker)
            {
                if (IsStarted)
                    IsStarted = false;
            }
        }

        public void RefreshUser(WorkingUser user)
        {
            lock (locker)
            {
                DataService?.GetWorkingUsers((data, ex) =>
                {
                /** Envio lo registrados */
                    Sessions.Broadcast(MessageService.RegisteredMsg(user));
                /** Envio los estados */
                    Sessions.Broadcast(MessageService.StatusMsg(user));
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
                if (IsStarted)
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
            return base.OnOpen();
        }
        #endregion Overrides

        #region Internal Data and Methods

        private Int32 RefreshPeriod { get; set; }
        private bool IsStarted { get; set; }
        private object locker = new object();
        
        #endregion Internal Data
    }
}
