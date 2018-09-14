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

        public static SIPResponse WR67ResponseNormalize(SIPResponse response)
        {
            response.Header.UnknownHeaders.Add("WG67-Version: phone.01");
            return response;
        }

        public static SIPAccount ServerAccount { get => s_serverAccount; set => s_serverAccount = value; }
        public static string ServerAdminId { get => s_serverAdminId; set => s_serverAdminId = value; }
    }

    public class SipPresenceSubscriptionManager
    {

        const int MAX_MANAGER_SLEEP_TIME = 1000;
        const int MAX_NOTIFIER_QUEUE_SIZE = 100;
        const int MIN_SUBSCRIPTION_EXPIRY = 60;

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
                    SIPResponse notSupportedResponse = SIPTransport.GetResponse(
                        subscribeRequest, SIPResponseStatusCodesEnum.MethodNotAllowed, "Subscribe requests only");
                    m_sipTransport.SendResponse(notSupportedResponse);
                }
                else
                {
                    #region Do as many validation checks as possible on the request before adding it to the queue.

                    if (subscribeRequest.Header.Event.IsNullOrBlank() ||
                        !(subscribeRequest.Header.Event.ToLower() == SIPEventPackage.Presence.ToString().ToLower()))
                    {
                        SIPResponse badEventResponse = SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.BadEvent, null);
                        m_sipTransport.SendResponse(badEventResponse);
                        logger.Warn("Event type " + subscribeRequest.Header.Event + " not supported for " + subscribeRequest.URI.ToString() + ".");
                    }
                    else if (subscribeRequest.Header.Expires > 0 && subscribeRequest.Header.Expires < MIN_SUBSCRIPTION_EXPIRY)
                    {
                        SIPResponse tooBriefResponse = SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.IntervalTooBrief, null);
                        tooBriefResponse.Header.MinExpires = MIN_SUBSCRIPTION_EXPIRY;
                        m_sipTransport.SendResponse(tooBriefResponse);
                        logger.Warn("Subscribe request was rejected as interval too brief " + subscribeRequest.Header.Expires + ".");
                    }
                    else if (subscribeRequest.Header.Contact == null || subscribeRequest.Header.Contact.Count == 0)
                    {
                        SIPResponse noContactResponse = SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.BadRequest, "Missing Contact header");
                        m_sipTransport.SendResponse(noContactResponse);
                        logger.Warn("Subscribe request was rejected due to no Contact header.");
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
                            logger.Info("Subscribe queued for " + subscribeRequest.Header.To.ToURI.ToString() + ".");
                        }
                        else
                        {
                            logger.Error("Subscribe queue exceeded max queue size " + MAX_NOTIFIER_QUEUE_SIZE + ", overloaded response sent.");
                            SIPResponse overloadedResponse = SIPTransport.GetResponse(subscribeRequest, SIPResponseStatusCodesEnum.TemporarilyUnavailable, "Notifier overloaded, please try again shortly");
                            m_sipTransport.SendResponse(overloadedResponse);
                        }
                        m_managerARE.Set();
                    }
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception AddNotifierRequest (" + remoteEndPoint.ToString() + "). " + excp.Message);
            }
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
                                logger.Info("Subscribe time=" + duration.TotalMilliseconds + "ms, user=" + subscribeTransaction.TransactionRequest.Header.To.ToURI.User + ".");
                            }
                        }
                        catch (Exception regExcp)
                        {
                            logger.Error("Exception ProcessSubscribeRequest Subscribe Job. " + regExcp.Message);
                        }
                    }
                    else
                    {
                        m_managerARE.WaitOne(MAX_MANAGER_SLEEP_TIME);
                    }
                }

                logger.Warn("ProcessSubscribeRequest thread " + Thread.CurrentThread.Name + " stopping.");
            }
            catch (Exception excp)
            {
                logger.Error("Exception ProcessSubscribeRequest (" + Thread.CurrentThread.Name + "). " + excp.Message);
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
                        SIPResponse renewalErrorResponse = SIPTransport.GetResponse(sipRequest, errorResponse, errorResponseReason);
                        subscribeTransaction.SendFinalResponse(renewalErrorResponse);
                        logger.Warn("Subscription renewal failed for event type " + sipRequest.Header.Event + " " + sipRequest.URI.ToString() + ", " + errorResponse + " " + errorResponseReason + ".");
                    }
                    else if (sipRequest.Header.Expires == 0)
                    {
                        // Existing subscription was closed.
                        SIPResponse okResponse = SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                        subscribeTransaction.SendFinalResponse(okResponse);
                    }
                    else
                    {
                        // Existing subscription was found.
                        SIPResponse okResponse = SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                        subscribeTransaction.SendFinalResponse(okResponse);
                        logger.Info("Subscription renewal for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + " and expiry " + sipRequest.Header.Expires + ".");
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
                        SIPResponse subscribeErrorResponse = SIPTransport.GetResponse(sipRequest, errorResponse, errorResponseReason);
                        subscribeTransaction.SendFinalResponse(subscribeErrorResponse);
                        logger.Warn("Subscription failed for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + ", " + errorResponse + " " + errorResponseReason + ".");
                    }
                    else
                    {
                        SIPResponse okResponse = SIPTransport.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);
                        okResponse.Header.To.ToTag = toTag;
                        okResponse.Header.Expires = sipRequest.Header.Expires;
                        okResponse.Header.Contact = new List<SIPContactHeader>() { new SIPContactHeader(null, new SIPURI(SIPSchemesEnum.sip, subscribeTransaction.LocalSIPEndPoint)) };
                        subscribeTransaction.SendFinalResponse(okResponse);
                        logger.Info("Subscription accepted for " + sipRequest.URI.ToString() + ", event type " + sipRequest.Header.Event + " and expiry " + sipRequest.Header.Expires + ".");

                        if (sessionID != null)
                        {
                            SendFullStateNotify(sessionID);
                        }
                    }
                }

            }
            catch (Exception excp)
            {
                logger.Error("Exception notifiercore subscribing. " + excp.Message + "\r\n" + subscribeTransaction.TransactionRequest.ToString());
                SIPResponse errorResponse = SIPTransport.GetResponse(subscribeTransaction.TransactionRequest, SIPResponseStatusCodesEnum.InternalServerError, null);
                subscribeTransaction.SendFinalResponse(errorResponse);
            }
        }

        public string SubscribeClient(
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
                        string subscribeError = null;
                        string sessionID = Guid.NewGuid().ToString();
                        SIPDialogue subscribeDialogue = new SIPDialogue(subscribeRequest, owner, adminID, toTag);

                        if (eventPackage == SIPEventPackage.Presence)
                        {
                            string monitorFilter = "presence " + canonicalResourceURI.ToString();
                            m_publisher.Subscribe(owner, adminID, m_notificationsAddress, sessionID, SIPMonitorClientTypesEnum.Machine.ToString(), monitorFilter, expiry, null, out subscribeError);

                            if (subscribeError != null)
                            {
                                throw new ApplicationException(subscribeError);
                            }
                            else
                            {
                                bool switchboardAccountsOnly = subscribeRequest.Body == SIPPresenceEventSubscription.SWITCHBOARD_FILTER;
                                SIPPresenceEventSubscription subscription = new SIPPresenceEventSubscription(MonitorLogEvent_External, sessionID, resourceURI, canonicalResourceURI, monitorFilter, subscribeDialogue, expiry, m_sipAssetPersistor.Get, m_sipAssetPersistor.GetProperty, GetSIPRegistrarBindingsCount_External, switchboardAccountsOnly);
                                m_subscriptions.Add(sessionID, subscription);
                                MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeAccept, "New presence subscription created for " + resourceURI.ToString() + ", expiry " + expiry + "s.", owner));
                            }
                        }

                        return sessionID;
                    }

                    return null;
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception NotifierSubscriptionsManager SubscribeClient. " + excp.Message);
                throw;
            }
        }

        public string RenewSubscription(SIPRequest subscribeRequest, out SIPResponseStatusCodesEnum errorResponse, out string errorReason)
        {
            errorResponse = SIPResponseStatusCodesEnum.None;
            errorReason = null;

            //try
            //{
            //    int expiry = subscribeRequest.Header.Expires;
            //    string toTag = subscribeRequest.Header.To.ToTag;
            //    string fromTag = subscribeRequest.Header.From.FromTag;
            //    string callID = subscribeRequest.Header.CallId;
            //    int cseq = subscribeRequest.Header.CSeq;

            //    // Check for an existing subscription.
            //    SIPEventSubscription existingSubscription = (from sub in m_subscriptions.Values where sub.SubscriptionDialogue.CallId == callID select sub).FirstOrDefault();

            //    if (existingSubscription != null)
            //    {
            //        if (expiry == 0)
            //        {
            //            // Subsciption is being cancelled.
            //            StopSubscription(existingSubscription);
            //            return null;
            //        }
            //        else if (cseq > existingSubscription.SubscriptionDialogue.RemoteCSeq)
            //        {
            //            logger.Debug("Renewing subscription for " + existingSubscription.SessionID + " and " + existingSubscription.SubscriptionDialogue.Owner + ".");
            //            existingSubscription.SubscriptionDialogue.RemoteCSeq = cseq;
            //            //existingSubscription.ProxySendFrom = (!subscribeRequest.Header.ProxyReceivedOn.IsNullOrBlank()) ? SIPEndPoint.ParseSIPEndPoint(subscribeRequest.Header.ProxyReceivedOn) : null;

            //            string extensionResult = m_publisher.ExtendSession(m_notificationsAddress, existingSubscription.SessionID, expiry);
            //            if (extensionResult != null)
            //            {
            //                // One or more of the monitor servers could not extend the session. Close all the existing sessions and re-create.
            //                MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeFailed, "Monitor session extension for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + " failed. " + extensionResult, existingSubscription.SubscriptionDialogue.Owner));
            //                m_publisher.CloseSession(m_notificationsAddress, existingSubscription.SessionID);

            //                // Need to re-establish the sessions with the notification servers.
            //                string subscribeError = null;
            //                string sessionID = Guid.NewGuid().ToString();
            //                m_publisher.Subscribe(existingSubscription.SubscriptionDialogue.Owner, existingSubscription.SubscriptionDialogue.AdminMemberId, m_notificationsAddress, sessionID, SIPMonitorClientTypesEnum.Machine.ToString(), existingSubscription.MonitorFilter, expiry, null, out subscribeError);

            //                if (subscribeError != null)
            //                {
            //                    throw new ApplicationException(subscribeError);
            //                }
            //                else
            //                {
            //                    lock (m_subscriptions)
            //                    {
            //                        m_subscriptions.Remove(existingSubscription.SessionID);
            //                        existingSubscription.SessionID = sessionID;
            //                        m_subscriptions.Add(sessionID, existingSubscription);
            //                    }
            //                    MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeAccept, "Monitor session recreated for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + ".", existingSubscription.SubscriptionDialogue.Owner));
            //                }
            //            }

            //            MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.SubscribeRenew, "Monitor session successfully renewed for " + existingSubscription.SubscriptionEventPackage.ToString() + " " + existingSubscription.ResourceURI.ToString() + ".", existingSubscription.SubscriptionDialogue.Owner));

            //            return existingSubscription.SessionID;
            //        }
            //        else
            //        {
            //            throw new ApplicationException("A duplicate SUBSCRIBE request was received by NotifierSubscriptionsManager.");
            //        }
            //    }
            //    else
            //    {
            //        //throw new ApplicationException("No existing subscription could be found for a subscribe renewal request.");
            //        errorResponse = SIPResponseStatusCodesEnum.CallLegTransactionDoesNotExist;
            //        errorReason = "Subscription dialog not found";
            //        return null;
            //    }
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception RenewSubscription. " + excp.Message);
            //    throw;
            //}
            return null;
        }

        public void SendFullStateNotify(string sessionID)
        {
            //try
            //{
            //    if (m_subscriptions.ContainsKey(sessionID))
            //    {
            //        SIPEventSubscription subscription = m_subscriptions[sessionID];

            //        lock (subscription)
            //        {
            //            subscription.GetFullState();
            //            SendNotifyRequestForSubscription(subscription);
            //        }
            //    }
            //    else
            //    {
            //        logger.Warn("No subscription could be found for " + sessionID + " when attempting to send a full state notification.");
            //    }
            //}
            //catch (Exception excp)
            //{
            //    logger.Error("Exception NotifierSubscriptionsManager SendFullStateNotify. " + excp.Message);
            //    throw;
            //}
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
        public const string SWITCHBOARD_FILTER = "switchboard";    // If a client specifies this value as a filter it's only interested in SIP accounts that are switchboard enabled.

        private static string m_wildcardUser = SIPMonitorFilter.WILDCARD;
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
                List<SIPAccount> sipAccounts = new List<SIPAccount>();
                sipAccounts.Add(SipHelper.ServerAccount);

                foreach (SIPAccount sipAccount in sipAccounts)
                {
                    SIPURI aor = SIPURI.ParseSIPURIRelaxed(sipAccount.SIPUsername + "@" + sipAccount.SIPDomain);

                    int bindingsCount = 1;  // OJO GetSIPRegistrarBindingsCount_External(b => b.SIPAccountId == sipAccount.Id);
                    if (bindingsCount > 0)
                    {
                        string safeSIPAccountID = sipAccount.Id.ToString();
                        Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, aor, Decimal.Zero, sipAccount.AvatarURL));
                    }
                    else
                    {
                        string safeSIPAccountID = sipAccount.Id.ToString();
                        Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.closed, null, Decimal.Zero, sipAccount.AvatarURL));
                    }
                }

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
            try
            {
                MonitorLogEvent_External(new SIPMonitorConsoleEvent(SIPMonitorServerTypesEnum.Notifier, SIPMonitorEventTypesEnum.Monitor, "Monitor event " + machineEvent.MachineEventType + " presence " + machineEvent.ResourceURI.ToString() + " for subscription to " + ResourceURI.ToString() + ".", SubscriptionDialogue.Owner));

                string safeSIPAccountID = machineEvent.ResourceID;
                SIPURI sipAccountURI = machineEvent.ResourceURI;
                bool sendNotificationForEvent = true;
                string avatarURL = null;

                if (m_switchboardSIPAccountsOnly)
                {
                    // Need to check whether the SIP account is switchboard enabled before forwarding the notification.
                    Guid sipAccountID = new Guid(machineEvent.ResourceID);
                    //sendNotificationForEvent = Convert.ToBoolean(m_sipAccountPersistor.GetProperty(sipAccountID, "IsSwitchboardEnabled"));
                    sendNotificationForEvent = Convert.ToBoolean(GetSipAccountProperty_External(sipAccountID, "IsSwitchboardEnabled"));

                    if (sendNotificationForEvent)
                    {
                        //avatarURL = m_sipAccountPersistor.GetProperty(sipAccountID, "AvatarURL") as string;
                        avatarURL = GetSipAccountProperty_External(sipAccountID, "AvatarURL") as string;
                    }
                }

                if (sendNotificationForEvent)
                {
                    if (machineEvent.MachineEventType == SIPMonitorMachineEventTypesEnum.SIPRegistrarBindingUpdate)
                    {
                        // A binding has been updated so there is at least one device online for the SIP account.
                        Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, sipAccountURI, Decimal.Zero, avatarURL));
                        //logger.Debug(" single presence open.");
                    }
                    else
                    {
                        // A binding has been removed but there could still be others.
                        Guid sipAccountID = new Guid(machineEvent.ResourceID);
                        int bindingsCount = GetSIPRegistrarBindingsCount_External(b => b.SIPAccountId == sipAccountID);
                        if (bindingsCount > 0)
                        {
                            Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.open, sipAccountURI, Decimal.Zero, avatarURL));
                        }
                        else
                        {
                            Presence.Tuples.Add(new SIPEventPresenceTuple(safeSIPAccountID, SIPEventPresenceStateEnum.closed, sipAccountURI, Decimal.Zero, avatarURL));
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception excp)
            {
                logger.Error("Exception SIPresenceEventSubscription AddMonitorEvent. " + excp.Message);
                throw;
            }
        }

        public override void NotificationSent()
        {
            Presence.Tuples.Clear();
        }
    }

}
