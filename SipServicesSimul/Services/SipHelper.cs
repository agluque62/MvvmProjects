using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;

//using SIPSorcery.CRM;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorcery.Sys;
using SIPSorcery.Sys.Net;
using log4net;

namespace SipServicesSimul.Services
{
    public class SipHelper
    {
        private static SIPAccount s_serverAccount = new SIPAccount("owner", "sipDomain", "username", "pwd", "outDialPlanName");
        private static string s_serverAdminId = "*";

        public static SIPResponse WG67ResponseNormalize(SIPResponse response)
        {
            response.Header.UnknownHeaders.Add("WG67-Version: phone.01");
            return response;
        }

        public static SIPRequest WG67RequestNormalice(SIPRequest request)
        {
            request.Header.UnknownHeaders.Add("WG67-Version: phone.01");
            return request;
        }

        public static SIPAccount ServerAccount { get => s_serverAccount; set => s_serverAccount = value; }
        public static string ServerAdminId { get => s_serverAdminId; set => s_serverAdminId = value; }

        public static Func<SimulPresenceEventSubscription, SIPEventPresenceStateEnum> IsUserOpen = null;
    }

    public class SipPresenceSubscriptionManager
    {
        public enum SipNotifierEvents { Info, Error, Subscribe, Unsubscribe, Notify}

        const int MAX_MANAGER_SLEEP_TIME = 1000;
        const int MAX_NOTIFIER_QUEUE_SIZE = 100;
        const int MIN_SUBSCRIPTION_EXPIRY = 60;

        #region Eventos...

        public event Action<SipNotifierEvents, string> InternalEvent;

        #endregion Eventos...

        #region Publics

        public SipPresenceSubscriptionManager(SIPTransport sipTransport)
        {
            m_sipTransport = sipTransport;
        }

        public void Start()
        {
            if (m_QueueManager == null)
            {
                m_QueueManager = Task.Factory.StartNew(() => ManagerQueueTask());
            }
        }

        public void Stop()
        {
            if (m_QueueManager != null)
            {
                m_QueueManager = null;
                m_managerARE.Set();
            }
        }

        public void AddSubscribeRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest subscribeRequest)
        {
            try
            {
                if (subscribeRequest.Method != SIPMethodsEnum.SUBSCRIBE)
                {
                    SIPResponse notSupportedResponse = SipHelper.WG67ResponseNormalize( 
                        SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.MethodNotAllowed, "Subscribe requests only"));
                    m_sipTransport.SendResponse(notSupportedResponse);

                    InternalEvent?.Invoke(SipNotifierEvents.Error, $"Method {subscribeRequest.Method} Not allowed.");
                }
                else
                {
                    #region Do as many validation checks as possible on the request before adding it to the queue.

                    if (subscribeRequest.Header.Event.IsNullOrBlank() ||
                        !(subscribeRequest.Header.Event.ToLower() == SIPEventPackage.Presence.ToString().ToLower()))
                    {
                        SIPResponse badEventResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.BadEvent, null));
                        m_sipTransport.SendResponse(badEventResponse);

                        InternalEvent?.Invoke(SipNotifierEvents.Error, 
                            "Event type " + subscribeRequest.Header.Event + " not supported for " + subscribeRequest.URI.ToString() + ".");
                    }
                    else if (subscribeRequest.Header.Expires > 0 && subscribeRequest.Header.Expires < MIN_SUBSCRIPTION_EXPIRY)
                    {
                        SIPResponse tooBriefResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.IntervalTooBrief, null));
                        tooBriefResponse.Header.MinExpires = MIN_SUBSCRIPTION_EXPIRY;
                        m_sipTransport.SendResponse(tooBriefResponse);

                        InternalEvent?.Invoke(SipNotifierEvents.Error, 
                            "Subscribe request was rejected as interval too brief " + subscribeRequest.Header.Expires + ".");
                    }
                    else if (subscribeRequest.Header.Contact == null || subscribeRequest.Header.Contact.Count == 0)
                    {
                        SIPResponse noContactResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.BadRequest, "Missing Contact header"));
                        m_sipTransport.SendResponse(noContactResponse);

                        InternalEvent?.Invoke(SipNotifierEvents.Error, 
                            "Subscribe request was rejected due to no Contact header.");
                    }
                    #endregion
                    else
                    {
                        if (m_managerQueue.Count < MAX_NOTIFIER_QUEUE_SIZE)
                        {
                            SIPNonInviteTransaction subscribeTransaction = m_sipTransport.CreateNonInviteTransaction(
                                subscribeRequest, remoteEndPoint, localSIPEndPoint, m_outboundProxy);
                            lock (m_managerQueue)
                            {
                                m_managerQueue.Enqueue(subscribeTransaction);
                            }

                            InternalEvent?.Invoke(SipNotifierEvents.Info, 
                                "Subscribe queued for " + subscribeRequest.Header.To.ToURI.ToString() + ".");
                        }
                        else
                        {
                            SIPResponse overloadedResponse = SipHelper.WG67ResponseNormalize(
                                SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.TemporarilyUnavailable, "Notifier overloaded, please try again shortly"));
                            m_sipTransport.SendResponse(overloadedResponse);

                            InternalEvent?.Invoke(SipNotifierEvents.Error, 
                                "Subscribe queue exceeded max queue size " + MAX_NOTIFIER_QUEUE_SIZE + ", overloaded response sent.");
                        }
                        m_managerARE.Set();
                    }
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error, "Exception AddNotifierRequest (" + remoteEndPoint.ToString() + "). " + excp.Message);
            }
        }

        public void RefreshNotify(string userid )
        {
            var subscriptions = (                
                from s in m_subscriptions
                where s.Value.ResourceURI.ToString().Contains(userid)
                select s).ToList();

            foreach(var s in subscriptions)
            {
                SendFullStateNotify(s.Key);
            }
        }

        public int UsersSubscriptions(string userid)
        {
            var subscriptions = (
                from s in m_subscriptions
                where s.Value.ResourceURI.ToString().Contains(userid)
                select s).ToList();

            return subscriptions.Count;
        }

        #endregion

        #region Internals

        private void ManagerQueueTask()
        {
            try
            {
                Thread.CurrentThread.Name = "SipPresenceSubscriptionManager.ManagerQueueTask";

                while (m_QueueManager != null)
                {
                    if (m_managerQueue.Count > 0)
                    {
                        try
                        {
                            SIPNonInviteTransaction subscribeTransaction = null;
                            lock (m_managerQueue)
                            {
                                subscribeTransaction = m_managerQueue.Dequeue();
                            }

                            if (subscribeTransaction != null)
                            {
                                DateTime startTime = DateTime.Now;
                                Subscribe(subscribeTransaction);
                                TimeSpan duration = DateTime.Now.Subtract(startTime);

                                InternalEvent?.Invoke(SipNotifierEvents.Info, 
                                    "Subscribe time=" + duration.TotalMilliseconds + "ms, user=" + subscribeTransaction.TransactionRequest.Header.To.ToURI.User + ".");
                            }
                        }
                        catch (Exception regExcp)
                        {
                            InternalEvent?.Invoke(SipNotifierEvents.Error, 
                                "Exception ProcessSubscribeRequest Subscribe Job. " + regExcp.Message);
                        }
                    }
                    else
                    {
                        /** Eliminar las subscripciones que lleguen a  su fin sin que sean refrescadas */
                        m_managerARE.WaitOne(MAX_MANAGER_SLEEP_TIME);
                    }
                }
                InternalEvent?.Invoke(SipNotifierEvents.Error, 
                    "ProcessSubscribeRequest thread " + Thread.CurrentThread.Name + " stopping.");
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error, "Exception ProcessSubscribeRequest (" + Thread.CurrentThread.Name + "). " + excp.Message);
            }
        }

        private void Subscribe(SIPTransaction subscribeTransaction)
        {
            try
            {
                SIPRequest sipRequest = subscribeTransaction.TransactionRequest;
                string fromUser = sipRequest.Header.From.FromURI.User;
                string fromHost = sipRequest.Header.From.FromURI.Host;

                if (sipRequest.Header.To.ToTag != null)
                {
                    // Request is to renew an existing subscription.
                    SIPResponseStatusCodesEnum errorResponse = SIPResponseStatusCodesEnum.None;
                    string errorResponseReason = null;

                    string sessionID = RenewSubscription(sipRequest, out errorResponse, out errorResponseReason);
                    if (errorResponse != SIPResponseStatusCodesEnum.None)
                    {
                        // A subscription renewal attempt failed
                        SIPResponse renewalErrorResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, errorResponse, errorResponseReason));
                        subscribeTransaction.SendFinalResponse(renewalErrorResponse);
                        InternalEvent?.Invoke(SipNotifierEvents.Error, 
                            "Subscription renewal failed for event type " + sipRequest.Header.Event + " " + sipRequest.URI.ToString() + ", " + errorResponse + " " + errorResponseReason + ".");
                    }
                    else if (sipRequest.Header.Expires == 0)
                    {
                        // Existing subscription was closed.
                        SIPResponse okResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null));
                        subscribeTransaction.SendFinalResponse(okResponse);
                        InternalEvent?.Invoke(SipNotifierEvents.Info, "Existing subscription was closed.");
                        /** TODO. Habrá que borrarla de la lista ???? */
                        InternalEvent?.Invoke(SipNotifierEvents.Unsubscribe, "????");
                    }
                    else
                    {
                        // Existing subscription was found.
                        SIPResponse okResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null));
                        subscribeTransaction.SendFinalResponse(okResponse);
                        InternalEvent?.Invoke(SipNotifierEvents.Info, 
                            "Subscription renewal for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + " and expiry " + sipRequest.Header.Expires + ".");
                        SendFullStateNotify(sessionID);
                    }
                }
                else
                {
                    // Request is to create a new subscription.
                    SIPResponseStatusCodesEnum errorResponse = SIPResponseStatusCodesEnum.None;
                    string errorResponseReason = null;
                    string toTag = CallProperties.CreateNewTag();
                    string sessionID = SubscribeClient(SipHelper.ServerAccount.Owner, SipHelper.ServerAdminId, 
                        sipRequest, toTag, out errorResponse, out errorResponseReason);

                    if (errorResponse != SIPResponseStatusCodesEnum.None)
                    {
                        SIPResponse subscribeErrorResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, errorResponse, errorResponseReason));
                        subscribeTransaction.SendFinalResponse(subscribeErrorResponse);
                        InternalEvent?.Invoke(SipNotifierEvents.Error, 
                            "Subscription failed for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + ", " + errorResponse + " " + errorResponseReason + ".");
                    }
                    else
                    {
                        SIPResponse okResponse = SipHelper.WG67ResponseNormalize(
                            SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null));
                        okResponse.Header.To.ToTag = toTag;
                        okResponse.Header.Expires = sipRequest.Header.Expires;
                        okResponse.Header.Contact = new List<SIPContactHeader>() { new SIPContactHeader(null, new SIPURI(SIPSchemesEnum.sip, subscribeTransaction.LocalSIPEndPoint)) };
                        subscribeTransaction.SendFinalResponse(okResponse);

                        InternalEvent?.Invoke(SipNotifierEvents.Info, 
                            "Subscription accepted for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + " and expiry " + sipRequest.Header.Expires + ".");

                        if (sessionID != null)
                        {
                            SendFullStateNotify(sessionID);
                        }

                        InternalEvent?.Invoke(SipNotifierEvents.Subscribe, sipRequest.URI.ToString());

                    }
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error,
                    "Exception notifiercore subscribing. " + excp.Message + "\r\n" + subscribeTransaction.TransactionRequest.ToString());

                SIPResponse errorResponse = SipHelper.WG67ResponseNormalize(
                    SIPTransport.GetResponse(subscribeTransaction.TransactionRequest, SIPResponseStatusCodesEnum.InternalServerError, null));
                subscribeTransaction.SendFinalResponse(errorResponse);
            }
        }

        private string SubscribeClient(
            string owner,
            string adminID,
            SIPRequest subscribeRequest,
            string toTag,
            //SIPURI canonicalResourceURI,
            out SIPResponseStatusCodesEnum errorResponse,
            out string errorReason)
        {
            errorResponse = SIPResponseStatusCodesEnum.None;
            errorReason = null;

            try
            {
                SIPURI resourceURI = subscribeRequest.URI.CopyOf();
                SIPEventPackage eventPackage = SIPEventPackage.Parse(subscribeRequest.Header.Event);
                int expiry = subscribeRequest.Header.Expires;

                if (!(eventPackage == SIPEventPackage.Presence))
                {
                    throw new ApplicationException("Event package " + eventPackage.ToString() + " is not supported by the subscriptions manager.");
                }
                else
                {
                    if (expiry > 0)
                    {
                        string sessionID = Guid.NewGuid().ToString();
                        SIPDialogue subscribeDialogue = new SIPDialogue(subscribeRequest, owner, adminID, toTag);

                        if (eventPackage == SIPEventPackage.Presence)
                        {
                            SimulPresenceEventSubscription subscription = new SimulPresenceEventSubscription(sessionID, resourceURI, subscribeDialogue, expiry);
                            m_subscriptions.Add(sessionID, subscription);
                            //string subscribeError = null;
                            //string monitorFilter = "presence " + canonicalResourceURI.ToString();
                            //m_publisher.Subscribe(owner, adminID, m_notificationsAddress, sessionID, SIPMonitorClientTypesEnum.Machine.ToString(), monitorFilter, expiry, null, out subscribeError);

                            //if (subscribeError != null)
                            //{
                            //    throw new ApplicationException(subscribeError);
                            //}
                            //else
                            //{
                            //    bool switchboardAccountsOnly = subscribeRequest.Body == SIPPresenceEventSubscription.SWITCHBOARD_FILTER;
                            //    SIPPresenceEventSubscription subscription = new SIPPresenceEventSubscription(MonitorLogEvent_External, sessionID, resourceURI, canonicalResourceURI, monitorFilter, subscribeDialogue, expiry, m_sipAssetPersistor.Get, m_sipAssetPersistor.GetProperty, GetSIPRegistrarBindingsCount_External, switchboardAccountsOnly);
                            //    m_subscriptions.Add(sessionID, subscription);
                            //    MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeAccept, "New presence subscription created for " + resourceURI.ToString() + ", expiry " + expiry + "s.", owner));
                            //}
                        }
                        return sessionID;
                    }
                    return null;
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error, 
                    "Exception NotifierSubscriptionsManager SubscribeClient. " + excp.Message);
                throw;
            }
        }

        private string RenewSubscription(SIPRequest subscribeRequest, out SIPResponseStatusCodesEnum errorResponse, out string errorReason)
        {
            errorResponse = SIPResponseStatusCodesEnum.None;
            errorReason = null;
            try
            {
                int expiry = subscribeRequest.Header.Expires;
                string toTag = subscribeRequest.Header.To.ToTag;
                string fromTag = subscribeRequest.Header.From.FromTag;
                string callID = subscribeRequest.Header.CallId;
                int cseq = subscribeRequest.Header.CSeq;

                // Check for an existing subscription.
                SIPEventSubscription existingSubscription = (from sub in m_subscriptions.Values where sub.SubscriptionDialogue.CallId == callID select sub).FirstOrDefault();

                if (existingSubscription != null)
                {
                    if (expiry == 0)
                    {
                        // Subsciption is being cancelled.
                        StopSubscription(existingSubscription);
                        return null;
                    }
                    else if (cseq > existingSubscription.SubscriptionDialogue.RemoteCSeq)
                    {
                        InternalEvent?.Invoke(SipNotifierEvents.Info, 
                            "Renewing subscription for " + existingSubscription.SessionID + " and " + existingSubscription.SubscriptionDialogue.Owner + ".");

                        existingSubscription.SubscriptionDialogue.RemoteCSeq = cseq;

                        string sessionID = Guid.NewGuid().ToString();
                        lock (m_subscriptions)
                        {
                            m_subscriptions.Remove(existingSubscription.SessionID);
                            existingSubscription.SessionID = sessionID;
                            
                            // OJO....
                            existingSubscription.Expiry = expiry;

                            m_subscriptions.Add(sessionID, existingSubscription);
                        }

                        ////existingSubscription.ProxySendFrom = (!subscribeRequest.Header.ProxyReceivedOn.IsNullOrBlank()) ? SIPEndPoint.ParseSIPEndPoint(subscribeRequest.Header.ProxyReceivedOn) : null;

                        //string extensionResult = m_publisher.ExtendSession(m_notificationsAddress, existingSubscription.SessionID, expiry);
                        //if (extensionResult != null)
                        //{
                        //    // One or more of the monitor servers could not extend the session. Close all the existing sessions and re-create.
                        //    MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeFailed, "Monitor session extension for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + " failed. " + extensionResult, existingSubscription.SubscriptionDialogue.Owner));
                        //    m_publisher.CloseSession(m_notificationsAddress, existingSubscription.SessionID);

                        //    // Need to re-establish the sessions with the notification servers.
                        //    string subscribeError = null;
                        //    string sessionID = Guid.NewGuid().ToString();
                        //    m_publisher.Subscribe(existingSubscription.SubscriptionDialogue.Owner, existingSubscription.SubscriptionDialogue.AdminMemberId, m_notificationsAddress, sessionID, SIPMonitorClientTypesEnum.Machine.ToString(), existingSubscription.MonitorFilter, expiry, null, out subscribeError);

                        //    if (subscribeError != null)
                        //    {
                        //        throw new ApplicationException(subscribeError);
                        //    }
                        //    else
                        //    {
                        //        lock (m_subscriptions)
                        //        {
                        //            m_subscriptions.Remove(existingSubscription.SessionID);
                        //            existingSubscription.SessionID = sessionID;
                        //            m_subscriptions.Add(sessionID, existingSubscription);
                        //        }
                        //        MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeAccept, "Monitor session recreated for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + ".", existingSubscription.SubscriptionDialogue.Owner));
                        //    }
                        //}

                        //MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeRenew, "Monitor session successfully renewed for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + ".", existingSubscription.SubscriptionDialogue.Owner));

                        InternalEvent?.Invoke(SipNotifierEvents.Info, 
                            "Monitor session successfully renewed for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + ".");

                        return existingSubscription.SessionID;
                    }
                    else
                    {
                        throw new ApplicationException("A duplicate SUBSCRIBE request was received by NotifierSubscriptionsManager.");
                    }
                }
                else
                {
                    //throw new ApplicationException("No existing subscription could be found for a subscribe renewal request.");
                    errorResponse = SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist;
                    errorReason = "Subscription dialog not found";
                    return null;
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error, 
                    "Exception RenewSubscription. " + excp.Message);
                throw;
            }
            //return null;
        }

        private void SendFullStateNotify(string sessionID)
        {
            try
            {
                if (m_subscriptions.ContainsKey(sessionID))
                {
                    SIPEventSubscription subscription = m_subscriptions[sessionID];

                    lock (subscription)
                    {
                        subscription.GetFullState();
                        SendNotifyRequestForSubscription(subscription);
                    }
                }
                else
                {
                    InternalEvent?.Invoke(SipNotifierEvents.Error, 
                        "No subscription could be found for " + sessionID + " when attempting to send a full state notification.");
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error, 
                    "Exception NotifierSubscriptionsManager SendFullStateNotify. " + excp.Message);
                throw;
            }
        }

        private void StopSubscription(SIPEventSubscription subscription)
        {
            try
            {
                //m_publisher.CloseSession(m_notificationsAddress, subscription.SessionID);
                lock (m_subscriptions)
                {
                    m_subscriptions.Remove(subscription.SessionID);
                }
                InternalEvent?.Invoke(SipNotifierEvents.Info,
                    "Stopping subscription for " + subscription.SubscriptionEventPackage.ToString() + " " + subscription.ResourceURI.ToString() + ".");
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error,
                    "Exception StopSubscription. " + excp.Message);
            }
        }

        private void SendNotifyRequestForSubscription(SIPEventSubscription subscription)
        {
            try
            {
                subscription.SubscriptionDialogue.CSeq++;

                //logger.Debug(DateTime.Now.ToString("HH:mm:ss:fff") + " Sending NOTIFY request for " + subscription.SubscriptionDialogue.Owner + " event " + subscription.SubscriptionEventPackage.ToString()
                //    + " and " + subscription.ResourceURI.ToString() + " to " + subscription.SubscriptionDialogue.RemoteTarget.ToString() + ", cseq=" + (subscription.SubscriptionDialogue.CSeq) + ".");

                int secondsRemaining = Convert.ToInt32(subscription.LastSubscribe.AddSeconds(subscription.Expiry).Subtract(DateTime.Now).TotalSeconds % Int32.MaxValue);

                SIPRequest notifyRequest = m_sipTransport.GetRequest(SIPMethodsEnum.NOTIFY, subscription.SubscriptionDialogue.RemoteTarget);
                notifyRequest.Header.From = SIPFromHeader.ParseFromHeader(subscription.SubscriptionDialogue.LocalUserField.ToString());
                notifyRequest.Header.To = SIPToHeader.ParseToHeader(subscription.SubscriptionDialogue.RemoteUserField.ToString());
                notifyRequest.Header.Event = subscription.SubscriptionEventPackage.ToString();
                notifyRequest.Header.CSeq = subscription.SubscriptionDialogue.CSeq;
                notifyRequest.Header.CallId = subscription.SubscriptionDialogue.CallId;
                notifyRequest.Body = subscription.GetNotifyBody();
                notifyRequest.Header.ContentLength = notifyRequest.Body.Length;
                notifyRequest.Header.SubscriptionState = "active;expires=" + secondsRemaining.ToString();
                notifyRequest.Header.ContentType = subscription.NotifyContentType;
                notifyRequest.Header.ProxySendFrom = subscription.SubscriptionDialogue.ProxySendFrom;

                notifyRequest = SipHelper.WG67RequestNormalice(notifyRequest);

                // If the outbound proxy is a loopback address, as it will normally be for local deployments, then it cannot be overriden.
                SIPEndPoint dstEndPoint = m_outboundProxy;
                if (m_outboundProxy != null && IPAddress.IsLoopback(m_outboundProxy.Address))
                {
                    dstEndPoint = m_outboundProxy;
                }
                //else if (subscription.SubscriptionDialogue.ProxySendFrom != null)
                //{
                //    // The proxy will always be listening on UDP port 5060 for requests from internal servers.
                //    dstEndPoint = new SIPEndPoint(SIPProtocolsEnum.udp, new IPEndPoint(SIPEndPoint.ParseSIPEndPoint(subscription.SubscriptionDialogue.ProxySendFrom).Address, m_defaultSIPPort));
                //}

                SIPNonInviteTransaction notifyTransaction = m_sipTransport.CreateNonInviteTransaction(notifyRequest, dstEndPoint, m_sipTransport.GetDefaultSIPEndPoint(dstEndPoint), m_outboundProxy);
                notifyTransaction.NonInviteTransactionFinalResponseReceived += (local, remote, transaction, response) => { NotifyTransactionFinalResponseReceived(local, remote, transaction, response, subscription); };
                m_sipTransport.SendSIPReliable(notifyTransaction);

                //logger.Debug(notifyRequest.ToString());

                //MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.NotifySent, "Notification sent for " + subscription.SubscriptionEventPackage.ToString() + " and " + subscription.ResourceURI.ToString() + " to " + subscription.SubscriptionDialogue.RemoteTarget.ToString() + " (cseq=" + notifyRequest.Header.CSeq + ").", subscription.SubscriptionDialogue.Owner));
                InternalEvent?.Invoke(SipNotifierEvents.Info,
                    "Notification sent for " + subscription.SubscriptionEventPackage.ToString() + " and " + subscription.ResourceURI.ToString() + " to " + subscription.SubscriptionDialogue.RemoteTarget.ToString() + " (cseq=" + notifyRequest.Header.CSeq + ").");

                subscription.NotificationSent();
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error,
                    "Exception SendNotifyRequestForSubscription. " + excp.Message);
                throw;
            }
        }

        private void NotifyTransactionFinalResponseReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPTransaction sipTransaction, SIPResponse sipResponse, SIPEventSubscription subscription)
        {
            try
            {
                if (sipResponse.StatusCode >= 300)
                {
                    // The NOTIFY request was met with an error response.
                    //MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.Warn, "A notify request received an error response of " + sipResponse.Status + " " + sipResponse.ReasonPhrase + ".", subscription.SubscriptionDialogue.Owner));
                    InternalEvent?.Invoke(SipNotifierEvents.Info,
                        "A notify request received an error response of " + sipResponse.Status + " " + sipResponse.ReasonPhrase + ".");
                    StopSubscription(subscription);
                }
            }
            catch (Exception excp)
            {
                InternalEvent?.Invoke(SipNotifierEvents.Error,
                    "Exception NotifyTransactionFinalResponseReceived. " + excp.Message);
            }
        }

        private bool MonitorEventAvailable(SIPMonitorEvent sipMonitorEvent)
        {
            //try
            //{
            //    SIPMonitorMachineEvent machineEvent = sipMonitorEvent as SIPMonitorMachineEvent;

            //    if (machineEvent != null && !machineEvent.SessionID.IsNullOrBlank() && m_subscriptions.ContainsKey(machineEvent.SessionID))
            //    {
            //        SIPEventSubscription subscription = m_subscriptions[machineEvent.SessionID];

            //        lock (subscription)
            //        {
            //            string resourceURI = (machineEvent.ResourceURI != null) ? machineEvent.ResourceURI.ToString() : null;

            //            //logger.Debug("NotifierSubscriptionsManager received new " + machineEvent.MachineEventType + ", resource ID=" + machineEvent.ResourceID + ", resource URI=" + resourceURI + ".");

            //            //MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.Monitor, "NotifierSubscriptionsManager received new " + machineEvent.MachineEventType + ", resource ID=" + machineEvent.ResourceID + ", resource URI=" + resourceURI + ".", subscription.SubscriptionDialogue.Owner));

            //            if (subscription.AddMonitorEvent(machineEvent))
            //            {
            //                SendNotifyRequestForSubscription(subscription);
            //            }

            //            //logger.Debug("NotifierSubscriptionsManager completed " + machineEvent.MachineEventType + ", resource ID=" + machineEvent.ResourceID + ", resource URI=" + resourceURI + ".");
            //        }

            //        return true;
            //    }

            //    return false;
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception NotifierSubscriptionsManager MonitorEventAvailable. " + excp.Message);
            //    return false;
            //}
            return false;
        }

        #endregion Internal

        #region Private

        Task m_QueueManager = null;

        private readonly SIPTransport m_sipTransport;
        // [monitor session ID, subscription].
        private readonly Dictionary<string, SIPEventSubscription> m_subscriptions = new Dictionary<string, SIPEventSubscription>();
        private readonly Queue<SIPNonInviteTransaction> m_managerQueue = new Queue<SIPNonInviteTransaction>();
        private readonly AutoResetEvent m_managerARE = new AutoResetEvent(false);

        private SIPEndPoint m_outboundProxy = null; // OJO....

        private static readonly ILog logger = AppState.logger;
        #endregion Private
    }

    public class SimulPresenceEventSubscription : SIPEventSubscription
    {
        private const int MAX_SIPACCOUNTS_TO_RETRIEVE = 25;
        // If a client specifies this value as a filter it's only interested in SIP accounts that are switchboard enabled.
        public const string SWITCHBOARD_FILTER = "switchboard";

        //private static string m_wildcardUser = SIPMonitorFilter.WILDCARD;
        private static string m_contentType = SIPMIMETypes.PRESENCE_NOTIFY_CONTENT_TYPE;

        private SIPEventPresence Presence;

        public override SIPEventPackage SubscriptionEventPackage
        {
            get { return SIPEventPackage.Presence; }
        }

        public override string MonitorFilter
        {
            get { return "presence " + ResourceURI.ToString(); }
        }

        public override string NotifyContentType
        {
            get { return m_contentType; }
        }

        public SimulPresenceEventSubscription(string sessionID, SIPURI resourceURI, SIPDialogue subscriptionDialogue,  int expiry )
            : base(null, sessionID, resourceURI, null, null, subscriptionDialogue, expiry)
        {
            Presence = new SIPEventPresence(resourceURI);
        }

        public override void GetFullState()
        {
            try
            {
                Presence.Tuples.Add(
                    new SIPEventPresenceTuple(
                        SipHelper.ServerAccount.Id.ToString(), 
                        SipHelper.IsUserOpen==null ? SIPEventPresenceStateEnum.closed : SipHelper.IsUserOpen(this),
                        null/*aor*/, 
                        Decimal.Zero, SipHelper.ServerAccount.AvatarURL));

                //List<SIPAccount> sipAccounts = new List<SIPAccount>();
                //sipAccounts.Add(SipHelper.ServerAccount);

                //foreach (SIPAccount sipAccount in sipAccounts)
                //{
                //    SIPURI aor = SIPURI.ParseSIPURIRelaxed(sipAccount.SIPUsername + "@" + sipAccount.SIPDomain);

                //    int bindingsCount = 1;  // OJO GetSIPRegistrarBindingsCount_External(b => b.SIPAccountId == sipAccount.Id);
                //    if (bindingsCount > 0)
                //    {
                //        string safeSIPAccountID = sipAccount.Id.ToString();
                //        Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, null/*aor*/, Decimal.Zero, sipAccount.AvatarURL));
                //    }
                //    else
                //    {
                //        string safeSIPAccountID = sipAccount.Id.ToString();
                //        Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.closed, null, Decimal.Zero, sipAccount.AvatarURL));
                //    }
                //}

                logger.Info("Full state notification for presence and " + ResourceURI.ToString() + ".");
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPPresenceEventSubscription GetFullState. " + excp.Message);
            }
        }

        public override string GetNotifyBody()
        {
            return Presence.ToXMLText();
        }

        /// <summary>
        /// Checks and where required adds a presence related monitor event to the list of pending notifications.
        /// </summary>
        /// <param name="machineEvent">The monitor event that has been received.</param>
        /// <returns>True if a notification needs to be sent as a result of this monitor event, false otherwise.</returns>
        public override bool AddMonitorEvent(SIPMonitorMachineEvent machineEvent)
        {
            //try
            //{
            //    MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.Monitor, "Monitor event " + machineEvent.MachineEventType + " presence " + machineEvent.ResourceURI.ToString() + " for subscription to " + ResourceURI.ToString() + ".", SubscriptionDialogue.Owner));

            //    string safeSIPAccountID = machineEvent.ResourceID;
            //    SIPURI sipAccountURI = machineEvent.ResourceURI;
            //    bool sendNotificationForEvent = true;
            //    string avatarURL = null;

            //    if (m_switchboardSIPAccountsOnly)
            //    {
            //        // Need to check whether the SIP account is switchboard enabled before forwarding the notification.
            //        Guid sipAccountID = new Guid(machineEvent.ResourceID);
            //        //sendNotificationForEvent = Convert.ToBoolean(m_sipAccountPersistor.GetProperty(sipAccountID, "IsSwitchboardEnabled"));
            //        sendNotificationForEvent = Convert.ToBoolean(GetSipAccountProperty_External(sipAccountID, "IsSwitchboardEnabled"));

            //        if (sendNotificationForEvent)
            //        {
            //            //avatarURL = m_sipAccountPersistor.GetProperty(sipAccountID, "AvatarURL") as string;
            //            avatarURL = GetSipAccountProperty_External(sipAccountID, "AvatarURL") as string;
            //        }
            //    }

            //    if (sendNotificationForEvent)
            //    {
            //        if (machineEvent.MachineEventType == SIPMonitorMachineEventTypesEnum.SIPRegistrarBindingUpdate)
            //        {
            //            // A binding has been updated so there is at least one device online for the SIP account.
            //            Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, sipAccountURI, Decimal.Zero, avatarURL));
            //            //logger.Debug(" single presence open.");
            //        }
            //        else
            //        {
            //            // A binding has been removed but there could still be others.
            //            Guid sipAccountID = new Guid(machineEvent.ResourceID);
            //            int bindingsCount = GetSIPRegistrarBindingsCount_External(b => b.SIPAccountId == sipAccountID);
            //            if (bindingsCount > 0)
            //            {
            //                Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, sipAccountURI, Decimal.Zero, avatarURL));
            //            }
            //            else
            //            {
            //                Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.closed, sipAccountURI, Decimal.Zero, avatarURL));
            //            }
            //        }
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception SIPresenceEventSubscription AddMonitorEvent. " + excp.Message);
            //    throw;
            //}
            return false;
        }

        public override void NotificationSent()
        {
            Presence.Tuples.Clear();
        }
    }

}
