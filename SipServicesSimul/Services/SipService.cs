using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using System.Text;
using System.Threading.Tasks;
using System.Net;

using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorcery.Sys;
using SIPSorcery.Sys.Net;
//using log4net;
using NuMvvmServices;

using SipServicesSimul.Model;

namespace SipServicesSimul.Services
{
    public interface ISipPresenceService
    {
        event Action<string> OptionsReceived;
        event Action<string> SubscribeReceived;
        event Action<Exception> ErrorOcurred;

        void Configure(IDataService dataService, Action<DataConfig, Exception> err);
        void Start(Action<Exception> err);
        void Stop(Action<Exception> err);

        void AddUser(string userid);
        void RemoveUser(string userid);

        int SubscriptionsTo(string userid);
    }

    public class SipPresenceService : ISipPresenceService
    {
        #region ISipPresenceService

        public event Action<string> /*ISipPresenceService.*/OptionsReceived;
        public event Action<string> /*ISipPresenceService.*/SubscribeReceived;

        event Action<Exception> ISipPresenceService.ErrorOcurred
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        void ISipPresenceService.AddUser(string userid)
        {
            if (_dataService != null)
            {
                _dataService.AddUser(userid);
                /** Si hay alguna subscripcion al usuario, hay que generar los NOTIFY correspondientes */
                m_sip_notifier.RefreshNotify(userid);
            }
        }

        void ISipPresenceService.Configure(IDataService dataService, Action<DataConfig, Exception> callback)
        {
            _dataService = dataService;
            _dataService.GetData((cfg, error) =>
            {
                if (error != null)
                {
                    callback?.Invoke(null, error);
                    return;
                }

                ListenIp = cfg.ListenIp;
                ListenPort = cfg.ListenPort;

                SipHelper.IsUserOpen = (s) =>
                {
                    foreach(var user in cfg.LastUsers)
                    {
                        if (s.ResourceURI.ToString().Contains(user.Id))
                            return SIPEventPresenceStateEnum.open;
                    }
                    return SIPEventPresenceStateEnum.closed;
                };

                var sipChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Parse(ListenIp), ListenPort));
                _sipTransport = new SIPTransport(SIPDNSManager.ResolveSIPService, new SIPTransactionEngine());
                _sipTransport.AddSIPChannel(sipChannel);

                _sipTransport.SIPTransportRequestReceived += SIPTransportRequestReceived;
                _sipTransport.SIPTransportResponseReceived += SIPTransportResponseReceived;

                m_sip_notifier = new SipPresenceSubscriptionManager(_sipTransport);

                m_sip_notifier.InternalEvent += (e, m) =>
                {
                    switch (e)
                    {
                        case SipPresenceSubscriptionManager.SipNotifierEvents.Error:
                            _logger.Error(m);
                            break;
                        case SipPresenceSubscriptionManager.SipNotifierEvents.Info:
                            _logger.Info(m);
                            break;
                        case SipPresenceSubscriptionManager.SipNotifierEvents.Notify:
                            break;
                        case SipPresenceSubscriptionManager.SipNotifierEvents.Subscribe:
                        case SipPresenceSubscriptionManager.SipNotifierEvents.Unsubscribe:
                            SubscribeReceived?.Invoke(m);
                            break;
                    }
                };

                callback?.Invoke(cfg, null);
            });
        }

        void ISipPresenceService.RemoveUser(string userid)
        {
            if (_dataService != null)
            {
                _dataService.DelUser(userid);
                /** Si hay suscripciones al usuario, hay que generar los correspondienes NOTIFY */
                m_sip_notifier.RefreshNotify(userid);
            }
            //throw new NotImplementedException();
        }

        void ISipPresenceService.Start(Action<Exception> err)
        {
            if (_dataService==null)
                err?.Invoke(new ApplicationException("El servicio no está configurado"));
            try
            {
                m_sip_notifier.Start();

                err?.Invoke(null);
            }
            catch (Exception x)
            {
                _logger.From().Debug($"SIPTransportRequestReceived Exception {x.Message}", x);
            }

        }

        void ISipPresenceService.Stop(Action<Exception> err)
        {
            if (_sipTransport == null)
                err?.Invoke(new NotImplementedException());
            try
            {
                m_sip_notifier.Stop();
                _sipTransport.Shutdown();
                err?.Invoke(null);
            }
            catch (Exception x)
            {
                err?.Invoke(x);
            }
        }

        public int SubscriptionsTo(string userid)
        {
            return m_sip_notifier.UsersSubscriptions(userid);
        }
        #endregion


        #region Private

        private string ListenIp { get; set; }
        private int ListenPort { get; set; }
        //private readonly ILog logger = AppState.logger;
        private IDataService _dataService = null;
        private ILogService _logger = new LogService();

        private SIPTransport _sipTransport = null;
        private SipPresenceSubscriptionManager m_sip_notifier = null;

        #region SIPSORCERY CALLBACKS

        private void SIPTransportRequestReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            try
            {
                switch (sipRequest.Method)
                {
                    case SIPMethodsEnum.OPTIONS:
                        SIPNonInviteTransaction optionsTransaction = _sipTransport.CreateNonInviteTransaction(sipRequest, remoteEndPoint, localSIPEndPoint, null);
                        SIPResponse optionsResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null));
                        optionsTransaction.SendFinalResponse(optionsResponse);
                        OptionsReceived?.Invoke(remoteEndPoint.ToString());
                        break;
                    case SIPMethodsEnum.SUBSCRIBE:
                        m_sip_notifier.AddSubscribeRequest(localSIPEndPoint, remoteEndPoint, sipRequest);
                        break;
                    case SIPMethodsEnum.PUBLISH:
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (NotImplementedException)
            {
                _logger.From().Debug(sipRequest.Method + " request processing not implemented for " + sipRequest.URI.ToParameterlessString() + " from " + remoteEndPoint + ".");

                SIPNonInviteTransaction notImplTransaction = _sipTransport.CreateNonInviteTransaction(sipRequest, remoteEndPoint, localSIPEndPoint, null);
                SIPResponse notImplResponse = SipHelper.WG67ResponseNormalize(SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.NotImplemented, null));
                notImplTransaction.SendFinalResponse(notImplResponse);
            }
            catch (Exception x)
            {
                _logger.From().Debug($"SIPTransportRequestReceived Exception {x.Message}", x);
            }
        }

        private void SIPTransportResponseReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {            
        }


        #endregion SIPSORCERY CALLBACKS


        #endregion Private
    }

}
