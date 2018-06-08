using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;
using CoreSipNet;

namespace RadioVoipSimV2.Model
{
    public class ControlledSipAgent
    {
        public void Init(CallIncomingCb onCallIncoming,
                CallStateCb onCallState, RdInfoCb onRdInfo, KaTimeoutCb onKaTimeout,
                OptionsReceiveCb onOptionsReceive, InfoReceivedCb onInfoReceived)
        {
            try
            {
                SipAgentNet.CallState += onCallState;
                SipAgentNet.CallIncoming += onCallIncoming;
                SipAgentNet.RdInfo += onRdInfo;
                SipAgentNet.KaTimeout += onKaTimeout;
                SipAgentNet.OptionsReceive += onOptionsReceive;
                SipAgentNet.InfoReceived += onInfoReceived;

                SipAgentNet.Log += (p1, p2, p3) =>
                {
                    if (p1 <= CoresipLogLevel)
                    {
                        LogManager.GetLogger("ControlledSipAgent").
                            Debug("CoreSipNet Log Level {0}: {1}", p1, p2);
                    }
                };

                SipAgentNet.Init(settings, "ROIPSIM",
                    IpBase,
                    SipPort);
            }
            catch (Exception x)
            {
                LogException(x, "Init Exception");
            }
        }
        public void Start()
        {
            try
            {
                SipAgentNet.Start();
            }
            catch (Exception x)
            {
                LogException(x, "Start Exception");
            }
        }
        public void End()
        {
            try
            {
                SipAgentNet.End();
            }
            catch (Exception x)
            {
                LogException(x, "End Exception");
            }
        }
        public void AnswerCall(int callid, int code)
        {
            try
            {
                SipAgentNet.AnswerCall(callid, code);
            }
            catch (Exception x)
            {
                LogException(x, "AnswerCall Exception");
            }
        }
        public void HangupCall(int callid, int code)
        {
            try
            {
                SipAgentNet.HangupCall(callid, code);
            }
            catch (Exception x)
            {
                LogException(x, "AnswerCall Exception");
            }
        }
        public void PttSet(int callid, CORESIP_PttType tipo, ushort pttId = 0)
        {
            try
            {
                if (tipo == CORESIP_PttType.CORESIP_PTT_OFF)
                    SipAgentNet.PttOff(callid);
                else
                    SipAgentNet.PttOn(callid, pttId, tipo);
            }
            catch (Exception x)
            {
                LogException(x, "PttSet Exception");
            }
        }
        public void SquelchSet(int callid, bool valor)
        {
            try
            {
                SipAgentNet.SqhOnOffSet(callid, valor);
            }
            catch (Exception x)
            {
                LogException(x, "SquelchSet Exception");
            }
        }
        public string SendOptionsMsg(string OptionsUri)
        {
            try
            {
                string callid = "";
                SipAgentNet.SendOptionsMsg(OptionsUri, out callid);
                return callid;
            }
            catch (Exception x)
            {
                LogException(x, "SendOptionsMsg Exception");
            }
            return "";
        }

        public int CoresipLogLevel { get; set; }
        public string IpBase { get; set; }
        public uint SipPort { get; set; }

        #region Protegidas.
        protected void LogException(Exception x, string msg, params object[] par)
        {
            LogManager.GetCurrentClassLogger().Error(x, msg);
        }
        protected  SipAgentNetSettings settings = new SipAgentNetSettings()
        {
            Default = new SipAgentNetSettings.DefaultSettings()
            {
                DefaultCodec = "PCMA",
                DefaultDelayBufPframes = 3,
                DefaultJBufPframes = 4,
                SndSamplingRate = 8000,
                RxLevel = 1,
                TxLevel = 1,
                SipLogLevel = 3,
                TsxTout = 400,
                InvProceedingIaTout = 1000,
                InvProceedingMonitoringTout = 30000,
                InvProceedingDiaTout = 30000,
                InvProceedingRdTout = 1000,
                KAPeriod = 200,
                KAMultiplier = 10
            }
        };
        #endregion
    }
}
