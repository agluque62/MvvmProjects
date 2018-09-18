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
using log4net;

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
    }

    public class SipPresenceService : ISipPresenceService
    {
        #region ISipPresenceService

        event Action<string> ISipPresenceService.OptionsReceived
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

        event Action<string> ISipPresenceService.SubscribeReceived
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
                /** TODO. Si hay alguna subscripcion al usuario, hay que generar los NOTIFY correspondientes */
            }
            //throw new NotImplementedException();
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

                callback?.Invoke(cfg, null);
            });
        }

        void ISipPresenceService.RemoveUser(string userid)
        {
            if (_dataService != null)
            {
                _dataService.DelUser(userid);
                /** TODO. Si hay suscripciones al usuario, hay que generar los correspondienes NOTIFY */
            }
            //throw new NotImplementedException();
        }

        void ISipPresenceService.Start(Action<Exception> err)
        {
            if (_dataService==null)
                err?.Invoke(new ApplicationException("El servicio no está configurado"));
            try
            {
                var sipChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Parse(ListenIp), ListenPort));
                _sipTransport = new SIPTransport(SIPDNSManager.ResolveSIPService, new SIPTransactionEngine());
                _sipTransport.AddSIPChannel(sipChannel);

                _sipTransport.SIPTransportRequestReceived += SIPTransportRequestReceived;
                _sipTransport.SIPTransportResponseReceived += SIPTransportResponseReceived;

                m_sip_notifier = new SipPresenceSubscriptionManager(_sipTransport);
                m_sip_notifier.Start();

                err?.Invoke(null);
            }
            catch (Exception x)
            {
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

        #endregion


        #region Private
        private string ListenIp { get; set; }
        private int ListenPort { get; set; }
        private readonly ILog logger = AppState.logger;
        private IDataService _dataService = null;

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
                logger.Debug(sipRequest.Method + " request processing not implemented for " + sipRequest.URI.ToParameterlessString() + " from " + remoteEndPoint + ".");

                SIPNonInviteTransaction notImplTransaction = _sipTransport.CreateNonInviteTransaction(sipRequest, remoteEndPoint, localSIPEndPoint, null);
                SIPResponse notImplResponse = SipHelper.WG67ResponseNormalize(SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.NotImplemented, null));
                notImplTransaction.SendFinalResponse(notImplResponse);
            }
            catch (Exception x)
            {
                logger.Debug($"SIPTransportRequestReceived Exception {x.Message}", x);
            }
        }

        private void SIPTransportResponseReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {
        }

        #endregion SIPSORCERY CALLBACKS

        
        #endregion Private
    }

}
