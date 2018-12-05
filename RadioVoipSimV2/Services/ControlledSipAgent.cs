using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreSipNet;
using NuMvvmServices;

namespace RadioVoipSimV2.Services
{

    public class ControlledSipAgent
    {
        #region Publicas.
        public enum SipAgentEvents { IncomingCall, CallConnected, CallDisconnected, PttOn, PttOff, KaTimeout }

        public void Init(int maxcall=32)
        {
            try
            {
                SipAgentNet.CallState += OnCallState;
                SipAgentNet.CallIncoming += OnCallIncoming;
                SipAgentNet.RdInfo += OnRdInfo;
                SipAgentNet.KaTimeout += OnKaTimeout;
                SipAgentNet.OptionsReceive += OnOptionsReceive;
                SipAgentNet.InfoReceived += OnInfoReceived;
                SipAgentNet.Log += OnLog;

                SipAgentNet.Init(settings, "ROIPSIM",
                    IpBase,
                    SipPort, (uint)maxcall);
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
                SipAgentNet.CallState -= OnCallState;
                SipAgentNet.CallIncoming -= OnCallIncoming;
                SipAgentNet.RdInfo -= OnRdInfo;
                SipAgentNet.KaTimeout -= OnKaTimeout;
                SipAgentNet.OptionsReceive -= OnOptionsReceive;
                SipAgentNet.InfoReceived -= OnInfoReceived;

                SipAgentNet.End();

                SipAgentNet.Log -= OnLog;
            }
            catch (Exception x)
            {
                LogException(x, "End Exception");
            }
            finally
            {
                SipAgentEvent = null;
            }
        }
        public void AnswerCall(int callid, int code)
        {
            try
            {
                _log.From().Trace($"AnswerCall {callid}, {code}");
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
                _log.From().Trace("HangupCall {0}, {1}", callid, code);
                SipAgentNet.HangupCall(callid, code);
            }
            catch (Exception x)
            {
                LogException(x, "AnswerCall Exception");
            }
        }
        public void PttSet(int callid, CORESIP_PttType tipo, ushort pttId = 0, int pttMute=0)
        {
            try
            {
                if (tipo == CORESIP_PttType.CORESIP_PTT_OFF)
                    SipAgentNet.PttOff(callid);
                else
                    SipAgentNet.PttOn(callid, pttId, tipo, pttMute);
            }
            catch (Exception x)
            {
                LogException(x, "PttSet Exception");
            }
        }
        public void SquelchSet(int callid, bool valor,
            CORESIP_PttType tipoptt = CORESIP_PttType.CORESIP_PTT_OFF, ushort pttId = 0)
        {
            try
            {
                SipAgentNet.SqhOnOffSet(callid, valor, tipoptt, pttId);
            }
            catch (Exception x)
            {
                LogException(x, "SquelchSet Exception");
            }
        }

        public int CreateWavPlayer(string file, bool loop)
        {
            int wavplayer = -1;
            try
            {
                wavplayer = SipAgentNet.CreateWavPlayer(file, loop);
            }
            catch (Exception x)
            {
                LogException(x, "CreateWavPlayer Exception");
                wavplayer = -1;
            }
            return wavplayer;
        }
        public void DestroyWavPlayer(int wavplayer)
        {
            try
            {
                SipAgentNet.DestroyWavPlayer(wavplayer);
            }
            catch (Exception x)
            {
                LogException(x, "DestroyWavPlayer Exception");
            }
        }
        public void MixerLink(int srcId, int dstId)
        {
            try
            {
                SipAgentNet.MixerLink(srcId, dstId);
            }
            catch (Exception x)
            {
                LogException(x, "MixerLink Exception");
            }
        }
        public void MixerUnlink(int srcId, int dstId)
        {
            try
            {
                SipAgentNet.MixerUnlink(srcId, dstId);
            }
            catch (Exception x)
            {
                LogException(x, "MixerUnlink Exception");
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

        public event Action<SipAgentEvents, int, string, CORESIP_RdInfo> SipAgentEvent=null;
        #endregion Publicas.

        #region Protegidas.
        protected void LogException(Exception x, string msg, params object[] par)
        {
            _log.From().Error(msg, x);
        }
        protected  	 settings = new SipAgentNetSettings()
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

        #region Callbacks
        private void OnCallIncoming(int call, int call2replace, CORESIP_CallInfo info, CORESIP_CallInInfo inInfo)
        {
            _log.From().Trace("OnCallIncoming {0}, {1}", call, inInfo.DstId);
            SipAgentEvent?.Invoke(SipAgentEvents.IncomingCall, call, inInfo.DstId, null);
        }
        private void OnCallState(int call, CORESIP_CallInfo info, CORESIP_CallStateInfo stateInfo)
        {
            _log.From().Trace("OnCallState {0}, {1}, {2}", call, stateInfo.State, stateInfo.LastCode);
            switch (stateInfo.State)
            {
                case CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED:
                    SipAgentEvent?.Invoke(SipAgentEvents.CallDisconnected, call, "", null);
                    break;
                case CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED:
                    SipAgentEvent?.Invoke(SipAgentEvents.CallConnected, call, "", null);
                    break;
                case CORESIP_CallState.CORESIP_CALL_STATE_INCOMING:
                case CORESIP_CallState.CORESIP_CALL_STATE_CALLING:
                case CORESIP_CallState.CORESIP_CALL_STATE_CONNECTING:
                case CORESIP_CallState.CORESIP_CALL_STATE_EARLY:
                case CORESIP_CallState.CORESIP_CALL_STATE_NULL:
                    break;
            }
        }
        private void OnRdInfo(int call, CORESIP_RdInfo info)
        {
            _log.From().Trace("OnRdInfo {0}, {1}, {2}, {3}", call, info.PttType, info.PttId, info.Squelch);
            switch (info.PttType)
            {
                case CORESIP_PttType.CORESIP_PTT_NORMAL:
                case CORESIP_PttType.CORESIP_PTT_PRIORITY:
                case CORESIP_PttType.CORESIP_PTT_EMERGENCY:
                case CORESIP_PttType.CORESIP_PTT_COUPLING:
                    SipAgentEvent?.Invoke(SipAgentEvents.PttOn, call, "", info);
                    break;

                case CORESIP_PttType.CORESIP_PTT_OFF:
                    SipAgentEvent?.Invoke(SipAgentEvents.PttOff, call, "", info);
                    break;
            }
        }
        private void OnKaTimeout(int call)
        {
            _log.From().Trace("OnKaTimeout {0}", call);
            SipAgentEvent?.Invoke(SipAgentEvents.KaTimeout, call, "", null);
        }
        private void OnOptionsReceive(string fromUri/*, string callid, int statusCodem, string supported, string allow*/)
        {
        }
        private void OnInfoReceived(int call, string info, uint lenInfo)
        {
        }
        private void OnLog(int p1, string p2, int p3)
        {
            if (p1 <= CoresipLogLevel)
            {
                if (p2.Contains("FrecDesp::GetQidx") == false)
                {
                    _log.From().Error("CoreSipNet Log Level {0}: {1}", p1, p2);
                }
            }
        }
        #endregion Callbacks

        private readonly ILogService _log = new LogService();
    }
}
