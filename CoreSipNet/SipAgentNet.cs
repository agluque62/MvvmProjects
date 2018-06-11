#define _ED137_
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Reflection;

namespace CoreSipNet
{
    public class SipAgentNetSettings
    {
        public class DefaultSettings
        {
            public string DefaultCodec { get; set; }
            public uint DefaultDelayBufPframes { get; set; }
            public uint DefaultJBufPframes { get; set; }
            public uint SndSamplingRate { get; set; }
            public float RxLevel { get; set; }
            public float TxLevel { get; set; }
            public uint SipLogLevel { get; set; }
            public uint TsxTout { get; set; }
            public uint InvProceedingIaTout { get; set; }
            public uint InvProceedingMonitoringTout { get; set; }
            public uint InvProceedingDiaTout { get; set; }
            public uint InvProceedingRdTout { get; set; }
            public uint KAPeriod { get; set; }
            public uint KAMultiplier { get; set; }
        }
        public DefaultSettings Default { get; set; }
    }

    #region delegados
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void LogCb(int level, string data, int len);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void KaTimeoutCb(int call);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void RdInfoCb(int call, [In] CORESIP_RdInfo info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallStateCb(int call, [In] CORESIP_CallInfo info, [In] CORESIP_CallStateInfo stateInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void CallIncomingCb(int call, int call2replace, [In] CORESIP_CallInfo info, [In] CORESIP_CallInInfo inInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void TransferRequestCb(int call, [In] CORESIP_CallInfo info, [In] CORESIP_CallTransferInfo transferInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void TransferStatusCb(int call, int code);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void ConfInfoCb(int call, [In] CORESIP_ConfInfo confInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void OptionsReceiveCb(string fromUri);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WG67NotifyCb(IntPtr wg67, CORESIP_WG67Info wg67Info, IntPtr userData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void InfoReceivedCb(int call, string info, uint lenInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void FinWavCb(int call);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UpdateOvrCallMembersCb([In] CORESIP_OvrCallMembers members);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void InfoCRDCb([In] CORESIP_CRD crd);


    #endregion

    #region enums
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_CallType
    {
        [Description("IA CALL")]
        CORESIP_CALL_IA,
        [Description("MON CALL")]
        CORESIP_CALL_MONITORING,
        [Description("G/G MON CALL")]
        CORESIP_CALL_GG_MONITORING,
        [Description("A/G MON CALL")]
        CORESIP_CALL_AG_MONITORING,
        [Description("DA CALL")]
        CORESIP_CALL_DIA,
        CORESIP_CALL_RD,

        /** De Coresip ETM*/
        CORESIP_CALL_RRC,
        CORESIP_CALL_OVR,
        CORESIP_CALL_RECORDING,
        CORESIP_CALL_RXTXRD,
        /**************************/

        CORESIP_CALL_UNKNOWN
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_Priority
    {
        [Description("Emergencia")]
        CORESIP_PR_EMERGENCY,
        [Description("Urgente")]
        CORESIP_PR_URGENT,
        [Description("Normal")]
        CORESIP_PR_NORMAL,
        [Description("No Urgente")]
        CORESIP_PR_NONURGENT,
        CORESIP_PR_UNKNOWN
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_CallFlags
    {
        [Description("TxRx")]
        CORESIP_CALL_NO_FLAGS = 0,
        [Description("Conference focus")]
        CORESIP_CALL_CONF_FOCUS = 0x1,
        [Description("Coupling")]
        CORESIP_CALL_RD_COUPLING = 0x2,
        [Description("Rx")]
        CORESIP_CALL_RD_RXONLY = 0x4,
        [Description("Tx")]
        CORESIP_CALL_RD_TXONLY = 0x8,
        [Description("Echo Canceller")]
        CORESIP_CALL_EC = 0x10,
        [Description("Through external central IP")]
        CORESIP_CALL_EXTERNAL_IP = 0x20,

        /** De Coresip ETM*/
        CORESIP_CALL_NINGUNO = 0x0, // Indica Transceiver
        CORESIP_CALL_RD_IDLE = 0x12,
        CORESIP_CALL_RD_TXRX = 0x14
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_CallState
    {
        [Description("Null")]
        CORESIP_CALL_STATE_NULL,					/**< Before INVITE is sent or received  */
        [Description("Calling")]
        CORESIP_CALL_STATE_CALLING,				/**< After INVITE is sent		    */
        [Description("Incoming")]
        CORESIP_CALL_STATE_INCOMING,				/**< After INVITE is received.	    */
        [Description("Early")]
        CORESIP_CALL_STATE_EARLY,					/**< After response with To tag.	    */
        [Description("Connecting")]
        CORESIP_CALL_STATE_CONNECTING,			/**< After 2xx is sent/received.	    */
        [Description("Confirmed")]
        CORESIP_CALL_STATE_CONFIRMED,			/**< After ACK is sent/received.	    */
        [Description("Disconnected")]
        CORESIP_CALL_STATE_DISCONNECTED,		/**< Session is terminated.		    */

        /** De Coresip ETM*/
        CORESIP_CALL_STATE_STATE_DESCONOCIDO,	/** < Estado DESCONOCIDO **/
        CORESIP_CALL_STATE_DISCONNECTED2		/**< Session LC (Tx) is terminated.		*/
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_CallRole
    {
        CORESIP_CALL_ROLE_UAC,
        CORESIP_CALL_ROLE_UCS
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_MediaStatus
    {
        CORESIP_MEDIA_NONE,
        CORESIP_MEDIA_ACTIVE,
        CORESIP_MEDIA_LOCAL_HOLD,
        CORESIP_MEDIA_REMOTE_HOLD,
        CORESIP_MEDIA_ERROR
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_MediaDir
    {
        CORESIP_DIR_NONE,
        CORESIP_DIR_SENDONLY,
        CORESIP_DIR_RECVONLY,
        CORESIP_DIR_SENDRECV
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_PttType
    {
        CORESIP_PTT_OFF,
        [Description("Normal")]
        CORESIP_PTT_NORMAL,
        [Description("Coupling")]
        CORESIP_PTT_COUPLING,
        [Description("Prioritario")]
        CORESIP_PTT_PRIORITY,
        [Description("Emergencia")]
        CORESIP_PTT_EMERGENCY
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_SndDevType
    {
        CORESIP_SND_INSTRUCTOR_MHP,
        CORESIP_SND_ALUMN_MHP,
        CORESIP_SND_MAX_IN_DEVICES,
        CORESIP_SND_MAIN_SPEAKERS = CORESIP_SND_MAX_IN_DEVICES,
        CORESIP_SND_LC_SPEAKER,
        CORESIP_SND_RD_SPEAKER,
        //CORESIP_SND_INSTRUCTOR_RECORDER,
        //CORESIP_SND_ALUMN_RECORDER,
        //CORESIP_SND_RADIO_RECORDER,
        //CORESIP_SND_LC_RECORDER,
        //CORESIP_SND_HF_SPEAKER,
        //CORESIP_SND_HF_RECORDER,
        CORESIP_SND_UNKNOWN
    }
    /// <summary>
    /// 
    /// </summary>
    public enum CORESIP_TypeCrdInfo
    {
        CORESIP_CRD_SET_PARAMETER,
        CORESIP_CRD_RECORD,
        CORESIP_CRD_PAUSE,
        CORESIP_CRD_PTT,
        CORESIP_SQ
    }

    #endregion

    #region structs
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CallInfo
    {
        public int AccountId;
        public CORESIP_CallType Type;
        public CORESIP_Priority Priority;
        public CORESIP_CallFlags Flags;

        /** */
        public CORESIP_CallFlags Flags_type;//12/2017
	    public CORESIP_MediaDir Dir;
	    public int Codecs;
	    public int BssMethods;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_FRECUENCY_LENGTH)]
        public string Frecuency;
//#if _VOTER_
//        /** 20160608. VOTER */
//        public int PreferredCodec = 0;
//        public int PreferredBss = 0;
//#endif
//        //EDU 20170223
//        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_ZONA_LENGTH + 1)]
//        public string Zona;
//        public CORESIP_FREQUENCY_TYPE FrequencyType;
//        public CORESIP_CLD_CALCULATE_METHOD CLDCalculateMethod;
//        public int BssWindows;
//        public bool AudioSync;
//        public bool AudioInBssWindow;
//        public bool NotUnassignable;
//        public int cld_supervision_time;
//        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgent.CORESIP_MAX_BSS_LENGTH + 1)]
//        public string bss_method;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CallOutInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string DstUri;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string ReferBy;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_RS_LENGTH + 1)]
        public string RdFr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_IP_LENGTH + 1)]
        public string RdMcastAddr;
        public uint RdMcastPort;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CallInInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string SrcId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_IP_LENGTH + 1)]
        public string SrcIp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string SrcSubId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_RS_LENGTH + 1)]
        public string SrcRs;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string DstId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_IP_LENGTH + 1)]
        public string DstIp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string DstSubId;
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_NAME_LENGTH + 1)]
        //public string DisplayName;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CallTransferInfo
    {
        public IntPtr TxData;
        public IntPtr EvSub;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_RS_LENGTH + 1)]
        public string TsxKey;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string ReferBy;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2 * SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string ReferTo;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string DstId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_IP_LENGTH + 1)]
        public string DstIp;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
        public string DstSubId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_RS_LENGTH + 1)]
        public string DstRs;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CallStateInfo
    {
        public CORESIP_CallState State;
        public CORESIP_CallRole Role;

        public int LastCode;										// Util cuando State == PJSIP_INV_STATE_DISCONNECTED
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_REASON_LENGTH + 1)]
        public string LastReason;

        public int LocalFocus;
        public int RemoteFocus;
        public CORESIP_MediaStatus MediaStatus;
        public CORESIP_MediaDir MediaDir;

        // CORESIP_CALL_RD y PJSIP_INV_STATE_CONFIRMED
        public ushort PttId;
        public uint ClkRate;
        public uint ChannelCount;
        public uint BitsPerSample;
        public uint FrameTime;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_PttInfo
    {
        public CORESIP_PttType PttType;
        public ushort PttId;
        public uint ClimaxCld;
        /** */
        public int Squelch;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_RdInfo
    {
        public CORESIP_PttType PttType;
        public ushort PttId;
        public int Squelch;
        public int Sct;

        ////EDU 20170224
        //public int rx_rtp_port;
        //public int rx_qidx;
        //public bool rx_selected;
        //public int tx_rtp_port;
        //public int tx_cld;
        //public int tx_owd;
        /** */
        public int Bss;
        public int BssMethod;
        public int BssValue;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_ConfInfo
    {
        public uint Version;
        public uint UsersCount;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_CONF_STATE_LENGTH + 1)]
        public string State;

        public struct ConfUser
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
            public string Id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
            public string Name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_USER_ID_LENGTH + 1)]
            public string Role;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_CONF_STATE_LENGTH + 1)]
            public string State;
        }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgentNet.CORESIP_MAX_CONF_USERS)]
        public ConfUser[] Users;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_WG67Info
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string DstUri;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_REASON_LENGTH + 1)]
        public string LastReason;

        public int SubscriptionTerminated;
        public uint SubscribersCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgentNet.CORESIP_MAX_WG67_SUBSCRIBERS)]
        public CORESIP_WG67SubscriberInfo[] Subscribers;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CORESIP_WG67SubscriberInfo
        {
            public ushort PttId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
            public string SubsUri;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_OvrCallMembers
    {
        public short MembersCount;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CORESIP_Members
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
            public string Member;
            public int CallId;
            bool IncommingCall;
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SipAgentNet.CORESIP_MAX_OVR_CALLS_MEMBERS)]
        public CORESIP_Members[] EstablishedOvrCallMembers;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_CRD
    {
        public CORESIP_TypeCrdInfo _Info;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_CALLREF_LENGTH + 1)]
        public string CallRef;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_CONNREF_LENGTH + 1)]
        public string ConnRef;
        public int Direction;
        public int Priority;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string CallingNr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string CallerNr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_TIME_LENGTH + 1)]
        public string SetupTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_URI_LENGTH + 1)]
        public string ConnectedNr;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_TIME_LENGTH + 1)]
        public string ConnectedTime;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_TIME_LENGTH + 1)]
        public string DisconnectTime;
        public int DisconnectCause;
        public int DisconnectSource;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_MAX_FRECUENCY_LENGTH1 + 1)]
        public string FrecuencyId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_TIME_LENGTH + 1)]
        public string Squ;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SipAgentNet.CORESIP_TIME_LENGTH + 1)]
        public string Ptt;
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_Callbacks
    {
        public IntPtr UserData;

        public LogCb OnLog;
        public KaTimeoutCb OnKaTimeout;
        public RdInfoCb OnRdInfo;
        public CallStateCb OnCallState;
        public CallIncomingCb OnCallIncoming;
        public TransferRequestCb OnTransferRequest;
        public TransferStatusCb OnTransferStatus;
        public ConfInfoCb OnConfInfo;
        public OptionsReceiveCb OnOptionsReceive;
        public WG67NotifyCb OnWG67Notify;
        public InfoReceivedCb OnInfoReceived;
        public FinWavCb OnFinWavCb;
        //public IncomingSubscribeConfCb OnIncomingSubscribeConf;
        //public SubPresCb OnSubPres;
#if _ED137_
	// PlugTest FAA 05/2011
		public UpdateOvrCallMembersCb OnUpdateOvrCallMembers; //(CORESIP_EstablishedOvrCallMembers info);
		public InfoCRDCb OnInfoCrd;								//(CORESIP_CRD InfoCrd);
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CORESIP_Params
    {
        public int EnableMonitoring;

        public uint KeepAlivePeriod;
        public uint KeepAliveMultiplier;
    }
    
    #endregion

    /// <summary>
    /// 
    /// </summary>
    public class SipAgentNet
    {
		public const int SIP_TRYING = 100;
		public const int SIP_RINGING = 180;
		public const int SIP_QUEUED = 182;
		public const int SIP_INTRUSION_IN_PROGRESS = 183;
		public const int SIP_INTERRUPTION_IN_PROGRESS = 184;
		public const int SIP_INTERRUPTION_END = 185;
		public const int SIP_OK = 200;
		public const int SIP_ACCEPTED = 202;
		public const int SIP_BAD_REQUEST = 400;
		public const int SIP_NOT_FOUND = 404;
		public const int SIP_REQUEST_TIMEOUT = 408;
		public const int SIP_GONE = 410;
		public const int SIP_TEMPORARILY_UNAVAILABLE = 480;
		public const int SIP_BUSY = 486;
		public const int SIP_NOT_ACCEPTABLE_HERE = 488;
		public const int SIP_ERROR = 500;
		public const int SIP_CONGESTION = 503;
		public const int SIP_DECLINE = 603;
        public const int SIP_UNWANTED = 607;

        //private static string UG5K_REC_CONF_FILE = "ug5krec-config.ini";

		#region Dll Interface

		public const int CORESIP_MAX_USER_ID_LENGTH = 100;
		public const int CORESIP_MAX_FILE_PATH_LENGTH = 256;
		public const int CORESIP_MAX_ERROR_INFO_LENGTH = 512;
        public const int CORESIP_MAX_HOSTID_LENGTH = 32;
		public const int CORESIP_MAX_IP_LENGTH = 25;
		public const int CORESIP_MAX_URI_LENGTH = 256;
		public const int CORESIP_MAX_SOUND_DEVICES = 10;
		public const int CORESIP_MAX_RS_LENGTH = 128;
		public const int CORESIP_MAX_REASON_LENGTH = 128;
		public const int CORESIP_MAX_WG67_SUBSCRIBERS = 25;
		public const int CORESIP_MAX_CODEC_LENGTH = 50;
		public const int CORESIP_MAX_CONF_USERS = 25;
		public const int CORESIP_MAX_CONF_STATE_LENGTH = 25;
        public const int CORESIP_MAX_ZONA_LENGTH = 256;     //EDU 20170223
        public const int CORESIP_MAX_BSS_LENGTH = 32;     //EDU 20170223
        public const int CORESIP_MAX_NAME_LENGTH = 20;    //B. Santamaria 20180206
        public const int CORESIP_MAX_CALLID_LENGTH = 256; 
   
        public const int CORESIP_MAX_FRECUENCY_LENGTH = CORESIP_MAX_RS_LENGTH + 1;
#if _ED137_
		// PlugTest FAA 05/2011
		public const int CORESIP_MAX_OVR_CALLS_MEMBERS = 10;
		public const int CORESIP_CALLREF_LENGTH = 50;
		public const int CORESIP_CONNREF_LENGTH = 50;
		public const int CORESIP_TIME_LENGTH = 28;
		public const int CORESIP_MAX_FRECUENCY_LENGTH1 = 7;
#endif
        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct CORESIP_Error
        {
            public int Code;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_FILE_PATH_LENGTH + 1)]
            public string File;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_ERROR_INFO_LENGTH + 1)]
            public string Info;
        }
        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        class CORESIP_SndDeviceInfo
        {
            public CORESIP_SndDevType Type;
            public int OsInDeviceIndex;
            public int OsOutDeviceIndex;
        }
        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        class CORESIP_RdRxPortInfo
        {
            public uint ClkRate;
            public uint ChannelCount;
            public uint BitsPerSample;
            public uint FrameTime;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_IP_LENGTH + 1)]
            public string Ip;
            public uint Port;
        }
        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        class CORESIP_Config
        {
            //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_HOSTID_LENGTH + 1)]
            //public string HostId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_IP_LENGTH + 1)]
            public string IpAddress;
            public uint Port;
            public CORESIP_Callbacks Cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CORESIP_MAX_CODEC_LENGTH + 1)]
            public string DefaultCodec;
            public uint DefaultDelayBufPframes;
            public uint DefaultJBufPframes;
            public uint SndSamplingRate;
            public float RxLevel;
            public float TxLevel;
            public uint LogLevel;

            public uint TsxTout;
            public uint InvProceedingIaTout;
            public uint InvProceedingMonitoringTout;
            public uint InvProceedingDiaTout;
            public uint InvProceedingRdTout;
            ///* AGL 20131121. Variables para la configuracion del Cancelador de Eco */
            //public uint EchoTail;
            //public uint EchoLatency;
            ///* FM */

            ///// <summary>
            ///// JCAM 18/01/2016
            ///// Grabación según norma ED-137
            ///// </summary>
            //public uint RecordingEd137;

            //public uint max_calls;		//Máximo número de llamadas que soporta el agente
        }
        /// <summary>
        /// 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        class CORESIP_Impairments
        {
            public int Perdidos;
            public int Duplicados;
            public int LatMin;
            public int LatMax;
        };

        #region Prototipos de funciones.
        const string coresip = "coresip";
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_Init([In] CORESIP_Config info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_Start(out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern void CORESIP_End();
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetLogLevel(uint level, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetParams([In] CORESIP_Params info, out CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateAccount([In] string acc, int defaultAcc, out int accId, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyAccount(int accId, out CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_AddSndDevice([In] CORESIP_SndDeviceInfo info, out int dev, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateRdRxPort([In] CORESIP_RdRxPortInfo info, string localIp, out int mcastPort, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyRdRxPort(int mcastPort, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateSndRxPort(string id, out int sndRxPort, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroySndRxPort(int sndRxPort, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_BridgeLink(int src, int dst, int on, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendToRemote(int dev, int on, string id, string ip, uint port, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_ReceiveFromRemote(string localIp, string mcastIp, uint mcastPort, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetVolume(int id, int volume, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_GetVolume(int dev, out int volume, out CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateWavPlayer([In] string file, int loop, out int wavPlayer, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyWavPlayer(int wavPlayer, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateWavRecorder([In] string file, out int wavPlayer, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyWavRecorder(int wavPlayer, out CORESIP_Error error);		

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallMake([In] CORESIP_CallInfo info, [In] CORESIP_CallOutInfo outInfo, out int call, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallHangup(int call, int code, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallAnswer(int call, int code, int addToConference, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallHold(int call, int hold, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallTransfer(int call, int dstCall, [In] string dst, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallPtt(int call, [In] CORESIP_PttInfo info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallConference(int call, int conf, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallSendConfInfo(int call, [In] CORESIP_ConfInfo info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallSendInfo(int call, [In] string info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_TransferAnswer(string tsxKey, IntPtr txData, IntPtr evSub, int code, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_TransferNotify(IntPtr evSub, int code, out CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CreateWG67Subscription(string dst, ref IntPtr wg67, ref CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_DestroyWG67Subscription(IntPtr wg67, ref CORESIP_Error error);

        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SendOptionsMsg([In] string dst, StringBuilder callid, out CORESIP_Error error);


        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_CallSq(int call, [In]  CORESIP_PttInfo info, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetTipoGRS(int accId, [In]  CORESIP_CallFlags flags, out CORESIP_Error error);
        [DllImport(coresip, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern int CORESIP_SetImpairments(int accId, [In]  CORESIP_Impairments flags, out CORESIP_Error error);  
        
        #endregion
        #endregion

        #region eventos
        static LogCb OnLog;
        public static event LogCb Log
        {
            add { OnLog += value; }
            remove { OnLog -= value; }
        }
#if _ED137_
        // PlugTest FAA 05/2011
        /// <summary>
        /// 
        /// </summary>
        static UpdateOvrCallMembersCb OnUpdateOvrCallMembers;
        public static event UpdateOvrCallMembersCb UpdateOvrCallMembers
        {
            add { OnUpdateOvrCallMembers += value; }
            remove { OnUpdateOvrCallMembers -= value; }
        }

        // PlugTest FAA 05/2011
        /// <summary>
        /// 
        /// </summary>
        static InfoCRDCb OnInfoCrd;
        public static event InfoCRDCb InfoCRD
        {
            add { OnInfoCrd += value; }
            remove { OnInfoCrd -= value; }
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        static InfoReceivedCb OnInfoReceived;
        public static event InfoReceivedCb InfoReceived
        {
            add {/*_Cb.*/OnInfoReceived += value; }
            remove {/*_Cb.*/OnInfoReceived -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static KaTimeoutCb OnKaTimeout;
        public static event KaTimeoutCb KaTimeout
        {
            add { /*_Cb.*/OnKaTimeout += value; }
            remove { /*_Cb.*/OnKaTimeout -= value; }
        }
        ///// <summary>
        ///// 
        ///// </summary>
        //static IncomingSubscribeConfCb OnIncomingSubscribeConf;
        //public static event IncomingSubscribeConfCb IncomingSubscribeConf
        //{
        //    add {/*_Cb.*/OnIncomingSubscribeConf += value; }
        //    remove {/*_Cb.*/OnIncomingSubscribeConf -= value; }
        //}
        /// <summary>
        /// 
        /// </summary>
        static RdInfoCb OnRdInfo;
        public static event RdInfoCb RdInfo
        {
            add { /*_Cb.*/OnRdInfo += value; }
            remove { /*_Cb.*/OnRdInfo -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static CallStateCb OnCallState;
        public static event CallStateCb CallState
        {
            add { /*_Cb.*/OnCallState += value; }
            remove { /*_Cb.*/OnCallState -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static CallIncomingCb OnCallIncoming;
        public static event CallIncomingCb CallIncoming
        {
            add {/*_Cb.*/OnCallIncoming += value; }
            remove {/*_Cb.*/OnCallIncoming -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static TransferRequestCb OnTransferRequest;
        public static event TransferRequestCb TransferRequest
        {
            add {/*_Cb.*/OnTransferRequest += value; }
            remove {/*_Cb.*/OnTransferRequest -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static TransferStatusCb OnTransferStatus;
        public static event TransferStatusCb TransferStatus
        {
            add {/*_Cb.*/OnTransferStatus += value; }
            remove {/*_Cb.*/OnTransferStatus -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        static ConfInfoCb OnConfInfo;
        public static event ConfInfoCb ConfInfo
        {
            add {/*_Cb.*/OnConfInfo += value; }
            remove {/*_Cb.*/OnConfInfo -= value; }
        }

        static WG67NotifyCb OnWG67Notify;
        public static event WG67NotifyCb WG67Notify
        {
            add {/*_Cb.*/OnWG67Notify += value; }
            remove {/*_Cb.*/OnWG67Notify -= value; }
        }

        ///
        //public static event ReplaceRequestCb ReplaceRequest
        //{
        //   add { _Cb.OnReplaceRequest += value; }
        //   remove { _Cb.OnReplaceRequest -= value; }
        //}
        /// <summary>
        /// 
        /// </summary>
        public static OptionsReceiveCb OnOptionsReceive;
        public static event OptionsReceiveCb OptionsReceive
        {
            add { /*_Cb.*/OnOptionsReceive += value; }
            remove { /*_Cb.*/OnOptionsReceive -= value; }
        }
        ///// <summary>
        ///// 
        ///// </summary>
        //static SubPresCb OnSubPres;
        //public static event SubPresCb SubPres
        //{
        //    add {/*_Cb.*/OnSubPres += value; }
        //    remove {/*_Cb.*/OnSubPres -= value; }
        //}
        #endregion

        #region Public Members
        static bool IsInitialized = false;
        static bool IsStarted = false;
        public static void Init(SipAgentNetSettings Settings, string accId, string ip, uint port, uint max_calls = 32, string proxyIP = null)
        {
            /** 20180208 */
            if (!IsInitialized)
            {
                IsInitialized = true;

                _Ip = ip;
                _Port = port;

                /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
                CallbacksInit();

                /** */
                CORESIP_Config cfg = new CORESIP_Config();

                //cfg.HostId = accId;     //GRABACION VOIP
                cfg.IpAddress = ip;
                cfg.Port = port;
                cfg.Cb = _Cb;
                cfg.DefaultCodec = Settings.Default.DefaultCodec;
                cfg.DefaultDelayBufPframes = Settings.Default.DefaultDelayBufPframes;
                cfg.DefaultJBufPframes = Settings.Default.DefaultJBufPframes;
                cfg.SndSamplingRate = Settings.Default.SndSamplingRate;
                cfg.RxLevel = Settings.Default.RxLevel;
                cfg.TxLevel = Settings.Default.TxLevel;
                cfg.LogLevel = Settings.Default.SipLogLevel;
                cfg.TsxTout = Settings.Default.TsxTout;
                cfg.InvProceedingIaTout = Settings.Default.InvProceedingIaTout;
                cfg.InvProceedingMonitoringTout = Settings.Default.InvProceedingMonitoringTout;
                cfg.InvProceedingDiaTout = Settings.Default.InvProceedingDiaTout;
                cfg.InvProceedingRdTout = Settings.Default.InvProceedingRdTout;

                // AGL 20131121.
                //cfg.EchoTail = Settings.Default.EchoTail;
                //cfg.EchoLatency = Settings.Default.EchoLatency;               // FM           
                ///// JCAM 18/01/2016
                ///// Grabación según norma ED-137
                //cfg.RecordingEd137 = Settings.Default.RecordingEd137;
                //cfg.max_calls = max_calls;     // Maximo número de llamadas por defecto en el puesto

                CORESIP_Error err;
                if (CORESIP_Init(cfg, out err) != 0)
                {
                    throw new Exception(err.Info);
                }

                CORESIP_Params sipParams = new CORESIP_Params();

                sipParams.EnableMonitoring = 0;
                sipParams.KeepAlivePeriod = Settings.Default.KAPeriod;
                sipParams.KeepAliveMultiplier = Settings.Default.KAMultiplier;

                SetParams(sipParams);
                SipAgentNet.CreateAccount(accId);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public static void Start()
        {
            /** 20180208 */
            if (!IsStarted)
            {
                CORESIP_Error err;
                if (CORESIP_Start(out err) != 0)
                {
                    throw new Exception(err.Info);
                }
                IsStarted = true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public static void End()
        {
            if (IsStarted || IsInitialized)
            {

                CORESIP_End();
                _Accounts.Clear();
                IsInitialized = IsStarted = false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
//        public static void SetLogLevel(LogLevel level)
//        {
//            uint eqLevel = 0;

//#if _TRACEAGENT_
//            _Logger.Debug("Entrando en SipAgent.SetLogLevel {0}", level.Name);
//#endif
//            if (level == LogLevel.Fatal)
//            {
//                eqLevel = 1;
//            }
//            else if (level == LogLevel.Error)
//            {
//                eqLevel = 2;
//            }
//            else if (level == LogLevel.Warn)
//            {
//                eqLevel = 3;
//            }
//            else if (level == LogLevel.Info)
//            {
//                eqLevel = 4;
//            }
//            else if (level == LogLevel.Debug)
//            {
//                eqLevel = 5;
//            }
//            else if (level == LogLevel.Trace)
//            {
//                eqLevel = 6;
//            }
//            CORESIP_Error err;
//            CORESIP_SetLogLevel(eqLevel, out err);
//#if _TRACEAGENT_
//            _Logger.Debug("Saliendo de SipAgent.SetLogLevel");
//#endif
//        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        public static void SetParams(CORESIP_Params cfg)
        {
            CORESIP_Error err;
            CORESIP_SetParams(cfg, out err);
        }
        /**
         *	CreateAccount. Registra una cuenta SIP en el Módulo. @ref SipAgent::CreateAccount
         *	@param	acc			Puntero al Numero de Abonado (usuario). NO a la uri.
         *	@param	defaultAcc	Marca si esta cuenta pasa a ser la Cuenta por Defecto.
         *	@param	accId		Puntero a el identificador de cuenta asociado.
         *	@param	error		Puntero @ref CORESIP_Error a la Estructura de error
         *	@return				Codigo de Error
         */
        public static void CreateAccount(string accId)
        {
            int id;
            CORESIP_Error err;
            string sipAcc = string.Format("<sip:{0}@{1}:{2}>", accId, _Ip, _Port);

            if (CORESIP_CreateAccount(sipAcc, 0, out id, out err) != 0)
            {
                throw new Exception(err.Info);
            }
            _Accounts[accId] = id;
        }
        /// <summary>
        /// 
        /// </summary>
        public static void DestroyAccounts()
        {
            foreach (int id in _Accounts.Values)
            {
                CORESIP_Error err;

                if (CORESIP_DestroyAccount(id, out err) != 0)
                {
                    throw new Exception("SipAgent.DestroyAccounts: " + err.Info);
                }
            }
            _Accounts.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inDevId"></param>
        /// <param name="outDevId"></param>
        /// <returns></returns>
        public static int AddSndDevice(CORESIP_SndDevType type, int inDevId, int outDevId)
        {
            CORESIP_SndDeviceInfo info = new CORESIP_SndDeviceInfo();

            info.Type = type;
            info.OsInDeviceIndex = inDevId;
            info.OsOutDeviceIndex = outDevId;

            int dev = 0;
            CORESIP_Error err;

            if (CORESIP_AddSndDevice(info, out dev, out err) != 0)
            {
                throw new Exception("SipAgent.AddSndDevice: " + err.Info);
            }

            return dev;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public static int CreateWavPlayer(string file, bool loop)
        {
            int wavPlayer;
            CORESIP_Error err;
            if (CORESIP_CreateWavPlayer(file, loop ? 1 : 0, out wavPlayer, out err) != 0)
            {
                throw new Exception("SipAgent.CreateWavPlayer Error: " + err.Info);
            }
            return wavPlayer;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="wavPlayer"></param>
        public static void DestroyWavPlayer(int wavPlayer)
        {
            CORESIP_Error err;
            if (CORESIP_DestroyWavPlayer(wavPlayer, out err) != 0)
            {
                throw new Exception("SipAgent.DestroyWavPlayer Error: " + err.Info);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        public static int CreateWavRecorder(string file)
        {
            int wavRecorder;
            CORESIP_Error err;
            if (CORESIP_CreateWavRecorder(file, out wavRecorder, out err) != 0)
            {
                throw new Exception("SipAgent.CreateWavRecorder Error: " + err.Info);
            }
            return wavRecorder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wavRecorder"></param>
        public static void DestroyWavRecorder(int wavRecorder)
        {
            CORESIP_Error err;
            if (CORESIP_DestroyWavRecorder(wavRecorder, out err) != 0)
            {
                throw new Exception("SipAgent.DestroyWavRecorder Error: " + err.Info);
            }
#if _TRACEAGENT_
            _Logger.Debug("Saliendo de SipAgent.DestroyWavRecorder");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="localIp"></param>
        /// <returns></returns>
//        public static int CreateRdRxPort(RdSrvRxRs rs, string localIp)
//        {
//#if _TRACEAGENT_
//            _Logger.Debug("Entrando en SipAgent.CreateRdRxPort {0}, {1}", rs.ToString(), localIp);
//#endif
//            CORESIP_RdRxPortInfo info = new CORESIP_RdRxPortInfo();

//            info.ClkRate = rs.ClkRate;
//            info.ChannelCount = rs.ChannelCount;
//            info.BitsPerSample = rs.BitsPerSample;
//            info.FrameTime = rs.FrameTime;
//            info.Ip = rs.McastIp;
//            info.Port = rs.RdRxPort;

//            int mcastPort;
//            CORESIP_Error err;

//            if (CORESIP_CreateRdRxPort(info, localIp, out mcastPort, out err) != 0)
//            {
//                throw new Exception(err.Info);
//            }
//#if _TRACEAGENT_
//            _Logger.Debug("Saliendo de SipAgent.CreateRdRxPort");
//#endif

//            return mcastPort;
//        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public static void DestroyRdRxPort(int port)
        {
            CORESIP_Error err;
            if (CORESIP_DestroyRdRxPort(port, out err) != 0)
            {
                throw new Exception("SipAgent.DestroyRdrxPort Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int CreateSndRxPort(string name)
        {
            int sndRxPort;
            CORESIP_Error err;
            if (CORESIP_CreateSndRxPort(name, out sndRxPort, out err) != 0)
            {
                throw new Exception("SipAgent.CreateSndRxPort Error: " + err.Info);
            }
            return sndRxPort;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        public static void DestroySndRxPort(int port)
        {
            CORESIP_Error err;
            if (CORESIP_DestroySndRxPort(port, out err) != 0)
            {
                throw new Exception("SipAgent.DestroySndRxPort: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        public static void MixerLink(int srcId, int dstId)
        {
            CORESIP_Error err;
            if (CORESIP_BridgeLink(srcId, dstId, 1, out err) != 0)
            {
                throw new Exception("SipAgent.MixerLink Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcId"></param>
        /// <param name="dstId"></param>
        public static void MixerUnlink(int srcId, int dstId)
        {
            CORESIP_Error err;
            if (CORESIP_BridgeLink(srcId, dstId, 0, out err) != 0)
            {
                throw new Exception("SipAgent.MixerUnlink Error: " + err.Info);
            }
        }
        /// <summary>
        /// Usado para la transmision del programa HMI hacia el nodebox-master. Cuando se hace PTT.
        /// </summary>
        /// <param name="sndDevId">Identificador de dispositivo asociado a un microfono</param>
        /// <param name="id">Identificador asociado al nodebox-master.</param>
        /// <param name="ip">Direccion mcast en la que escucha el nodebox-master</param>
        /// <param name="port">Puesto UDP asociado al grupo mcast donde escucha el nodebox-master.</param>
        public static void SendToRemote(int sndDevId, string id, string ip, uint port)
        {
            CORESIP_Error err;
            if (CORESIP_SendToRemote(sndDevId, 1, id, ip, port, out err) != 0)
            {
                throw new Exception("SipAgent.SendToRemote Error: " + err.Info);
            }
        }
        /// <summary>
        /// Usado por el nodebox-master para recibir tramas del HMI....
        /// </summary>
        /// <param name="localIp"></param>
        /// <param name="mcastIp"></param>
        /// <param name="mcastPort"></param>
        public static void ReceiveFromRemote(string localIp, string mcastIp, uint mcastPort)
        {
            CORESIP_Error err;
            if (CORESIP_ReceiveFromRemote(localIp, mcastIp, mcastPort, out err) != 0)
            {
                throw new Exception("SipAgent.ReceiveFromRemote Error: " + err.Info);
            }
        }
        /// <summary>
        /// Usado por el HMI para 'redireccionar' su transmision. YA no va hacia el nodebox-master. (por ejemplo para cuando utiliza telefonia).
        /// </summary>
        /// <param name="sndDevId">Identificador del dispositivo asociado a un micrófono.</param>
        public static void UnsendToRemote(int sndDevId)
        {
            CORESIP_Error err;
            if (CORESIP_SendToRemote(sndDevId, 0, null, null, 0, out err) != 0)
            {
                throw new Exception("SipAgent.UnsendToRemote: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="volume"></param>
        public static void SetVolume(int id, int volume)
        {
            CORESIP_Error err;
            if (CORESIP_SetVolume(id, volume, out err) != 0)
            {
                throw new Exception("SipAgent.SetVolume Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static int GetVolume(int id)
        {
            int volume;
            CORESIP_Error err;
            if (CORESIP_GetVolume(id, out volume, out err) != 0)
            {
                throw new Exception("SipAgent.GetVolume Error: " + err.Info);
            }
            return volume;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="referBy"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static int MakeTlfCall(string accId, string dst, string referBy, CORESIP_Priority priority)
        {
            int retorno = MakeTlfCall(accId, dst, referBy, priority, CORESIP_CallFlags.CORESIP_CALL_NO_FLAGS);
            return retorno;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="referBy"></param>
        /// <param name="priority"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static int MakeTlfCall(string accId, string dst, string referBy, CORESIP_Priority priority, CORESIP_CallFlags flags)
        {
            int acc;
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
            {
                acc = -1;
            }

            CORESIP_CallInfo info = new CORESIP_CallInfo();
            CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
            info.Type = CORESIP_CallType.CORESIP_CALL_DIA;
            info.Priority = priority;
            info.Flags = flags;

            outInfo.DstUri = dst;
            outInfo.ReferBy = referBy;

            int callId;
            CORESIP_Error err;

            if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
            {
                throw new Exception("SipAgent.MakeTlfCall Error: " + err.Info);
            }
            return callId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static int MakeLcCall(string accId, string dst, CORESIP_CallFlags flags)
        {
            int acc;
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
            {
                acc = -1;
            }

            CORESIP_CallInfo info = new CORESIP_CallInfo();
            CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
            info.Type = CORESIP_CallType.CORESIP_CALL_IA;
            info.Priority = CORESIP_Priority.CORESIP_PR_URGENT;
            info.Flags = flags;

            outInfo.DstUri = dst;

            int callId;
            CORESIP_Error err;

            if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
            {
                throw new Exception("SipAgent.MakeLcCall Error: " + err.Info);
            }
            return callId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="frecuency"></param>
        /// <param name="flags"></param>
        /// <param name="mcastIp">Grupo Multicast de Recepcion para los HMI.</param>
        /// <param name="mcastPort">Puerto del grupo asociado al recurso radio.</param>
        /// <returns></returns>
        public static int MakeRdCall(string accId, string dst, string frecuency, CORESIP_CallFlags flags, string mcastIp, uint mcastPort,
            CORESIP_Priority prioridad
            //, string Zona, CORESIP_FREQUENCY_TYPE FrequencyType,
            //CORESIP_CLD_CALCULATE_METHOD CLDCalculateMethod, int BssWindows, bool AudioSync, bool AudioInBssWindow, bool NotUnassignable,
            //int cld_supervision_time, string bss_method
            )
        {
            int acc;
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
            {
                acc = -1;
            }

            CORESIP_CallInfo info = new CORESIP_CallInfo();
            CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
            info.Type = CORESIP_CallType.CORESIP_CALL_RD;
            /* AGL*/
            info.Priority = prioridad;  // CORESIP_Priority.CORESIP_PR_EMERGENCY;
            info.Flags = flags;
//#if _VOTER_
//            /** 20160609 */
//            info.PreferredBss = 0;          // Globales.IndexOfPreferredBss;
//            info.PreferredCodec = 0;        // Globales.IndexOfPreferredCodec;
//#endif
//            //EDU 20170223
//            info.Zona = Zona;
//            info.FrequencyType = FrequencyType;
//            info.CLDCalculateMethod = CLDCalculateMethod;
//            info.BssWindows = BssWindows;
//            info.AudioSync = AudioSync;
//            info.AudioInBssWindow = AudioInBssWindow;
//            info.NotUnassignable = NotUnassignable;
//            info.cld_supervision_time = cld_supervision_time;
//            info.bss_method = bss_method;

            outInfo.DstUri = dst;
            outInfo.RdFr = frecuency;
            outInfo.RdMcastAddr = mcastIp;
            outInfo.RdMcastPort = mcastPort;

            int callId;
            CORESIP_Error err;

            if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
            {
                throw new Exception("SipAgent.MakeRdCall: [" + outInfo.DstUri + "]" + err.Info);
            }
            return callId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static int MakeMonitoringCall(string accId, string dst, CORESIP_CallFlags flags)
        {
            return MakeMonitoringCall(accId, dst, CORESIP_CallType.CORESIP_CALL_MONITORING, flags);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accId"></param>
        /// <param name="dst"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int MakeMonitoringCall(string accId, string dst, CORESIP_CallType type, CORESIP_CallFlags flags)
        {
            int acc;
            if (string.IsNullOrEmpty(accId) || !_Accounts.TryGetValue(accId, out acc))
            {
                acc = -1;
            }

            CORESIP_CallInfo info = new CORESIP_CallInfo();
            CORESIP_CallOutInfo outInfo = new CORESIP_CallOutInfo();

            info.AccountId = acc;
            info.Type = type;
            info.Priority = CORESIP_Priority.CORESIP_PR_NONURGENT;  // .CORESIP_PR_NORMAL;
            info.Flags = flags;

            outInfo.DstUri = dst;

            int callId;
            CORESIP_Error err;

            if (CORESIP_CallMake(info, outInfo, out callId, out err) != 0)
            {
                throw new Exception("SipAgent.MakeMonitoringCall Error: " + err.Info);
            }
            return callId;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void HangupCall(int callId)
        {
            CORESIP_Error err;
            if (CORESIP_CallHangup(callId, 0, out err) != 0)
            {
                throw new Exception("SipAgent.HangupCall Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="code"></param>
        public static void HangupCall(int callId, int code)
        {
            CORESIP_Error err;
            if (CORESIP_CallHangup(callId, code, out err) != 0)
            {
                throw new Exception("SipAgent.HangupCall_2: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="response"></param>
        public static void AnswerCall(int callId, int response)
        {
            AnswerCall(callId, response, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="response"></param>
        /// <param name="addToConference"></param>
        public static void AnswerCall(int callId, int response, bool addToConference)
        {
            CORESIP_Error err;
            if (CORESIP_CallAnswer(callId, response, addToConference ? 1 : 0, out err) != 0)
            {
                throw new Exception("SipAgent.AnswerCall Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void HoldCall(int callId)
        {
            CORESIP_Error err;
            if (CORESIP_CallHold(callId, 1, out err) != 0)
            {
                throw new Exception("SipAgent.HoldCall Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void UnholdCall(int callId)
        {
            CORESIP_Error err;
            if (CORESIP_CallHold(callId, 0, out err) != 0)
            {
                throw new Exception("SipAgent.UnholdCall Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="dstCallId"></param>
        /// <param name="dst"></param>
        public static void TransferCall(int callId, int dstCallId, string dst)
        {
            CORESIP_Error err;
            if (CORESIP_CallTransfer(callId, dstCallId, dst, out err) != 0)
            {
                throw new Exception("SipAgent.TransferCall Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="pttId"></param>
        /// <param name="pttType"></param>
        public static void PttOn(int callId, ushort pttId, CORESIP_PttType pttType)
        {
            CORESIP_PttInfo info = new CORESIP_PttInfo();

            info.PttType = pttType;
            info.PttId = pttId;

            CORESIP_Error err;

            if (CORESIP_CallPtt(callId, info, out err) != 0)
            {
                throw new Exception("SipAgent.PttOn Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void PttOff(int callId)
        {
            CORESIP_PttInfo info = new CORESIP_PttInfo();
            info.PttType = CORESIP_PttType.CORESIP_PTT_OFF;

            CORESIP_Error err;

            if (CORESIP_CallPtt(callId, info, out err) != 0)
            {
                throw new Exception("SipAgent.PttOff Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void AddCallToConference(int callId)
        {
            CORESIP_Error err;
            if (CORESIP_CallConference(callId, 1, out err) != 0)
            {
                throw new Exception("SipAgent.AddCallToConference Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        public static void RemoveCallFromConference(int callId)
        {
            CORESIP_Error err;
            if (CORESIP_CallConference(callId, 0, out err) != 0)
            {
                throw new Exception("SipAgent.RemoveCallFromConference Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="info"></param>
        public static void SendConfInfo(int callId, CORESIP_ConfInfo info)
        {
            CORESIP_Error err;
            if (CORESIP_CallSendConfInfo(callId, info, out err) != 0)
            {
                throw new Exception("SipAgent.SendConfInfo Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="info"></param>
        public static void SendInfo(int callId, string info)
        {
            CORESIP_Error err;
            if (CORESIP_CallSendInfo(callId, info, out err) != 0)
            {
                throw new Exception("SipAgent.SendInfo Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst"></param>
        public static void SendOptionsMsg(string dst, out string callid)
        {
            CORESIP_Error err;
            StringBuilder callid_ = new StringBuilder(CORESIP_MAX_CALLID_LENGTH + 1);
            if (CORESIP_SendOptionsMsg(dst, callid_, out err) != 0)
            {
                throw new Exception("SipAgent.SendOptionsMsg Error: " + err.Info);
            }
            callid = callid_.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tsxKey"></param>
        /// <param name="txData"></param>
        /// <param name="evSub"></param>
        /// <param name="code"></param>
        public static void TransferAnswer(string tsxKey, IntPtr txData, IntPtr evSub, int code)
        {
            CORESIP_Error err;
            if (CORESIP_TransferAnswer(tsxKey, txData, evSub, code, out err) != 0)
            {
                throw new Exception("SipAgent.TransferAnswer Error: " + err.Info);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evSub"></param>
        /// <param name="code"></param>
        public static void TransferNotify(IntPtr evSub, int code)
        {
            CORESIP_Error err;
            if (CORESIP_TransferNotify(evSub, code, out err) != 0)
            {
                throw new Exception("SipAgent.TransferNotify Error: " + err.Info);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static IntPtr CreateWG67Subscription(string dst)
        {
            IntPtr wg67 = IntPtr.Zero;
            CORESIP_Error err = new CORESIP_Error();

            if (CORESIP_CreateWG67Subscription(dst, ref wg67, ref err) != 0)
            {
                throw new Exception("Error creating WG67 KEY-IN Subscription: " + err.Info);
            }
            return wg67;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wg67"></param>
        public static void DestroyWG67Subscription(IntPtr wg67)
        {
            CORESIP_Error err = new CORESIP_Error();

            if (CORESIP_DestroyWG67Subscription(wg67, ref err) != 0)
            {
                throw new Exception("Error destroying WG67 KEY-IN Subscription: " + err.Info);
            }
        }

        /** AGL */
//        public static void Wav2Remote(string file, string id, string ip, int port)
//        {
//#if _TRACEAGENT_
//            _Logger.Debug("Entrando en SipAgent.WavToRemote {0}, {1}, {2}, {3}", file, id, ip, port);
//#endif
//            CORESIP_Error err = new CORESIP_Error();
//            WavRemoteEnd cb = new WavRemoteEnd(Wav2RemoteEnd);
//            CORESIP_Wav2RemoteStart(file, id, ip, port, ref cb, ref err);
//#if _TRACEAGENT_
//            _Logger.Debug("Saliendo de SipAgent.Wav2Remote");
//#endif
//        }

//        /** */
//        public static void Wav2RemoteEnd(IntPtr obj)
//        {
//#if _TRACEAGENT_
//            _Logger.Debug("Entrando en SipAgent.WavToRemoteEnd {0}", obj);
//#endif
//            CORESIP_Error err = new CORESIP_Error();
//            CORESIP_Wav2RemoteEnd(obj, ref err);
//            /** Meter un Evento.... */
//#if _TRACEAGENT_
//            _Logger.Debug("Saliendo de SipAgent.WavToRemoteEnd");
//#endif
//        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callId"></param>
        /// <param name="sqh"></param>
        public static void SqhOnOffSet(int callId, bool sqh)
        {
            CORESIP_Error err;
            CORESIP_PttInfo info = new CORESIP_PttInfo()
            {
                Squelch = sqh ? 1 : 0
            };


            if (CORESIP_CallSq(callId, info, out err) != 0)
            {
                throw new Exception("SipAgent.PttOff SqhOnOffSet: " + err.Info);
            }
        }

        #endregion
        //


        #region Private Members
        /// <summary>
        /// 
        /// </summary>
        // private static Logger _Logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
        private static CORESIP_Callbacks _Cb = new CORESIP_Callbacks();
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<string, int> _Accounts = new Dictionary<string, int>();
        /// <summary>
        /// 
        /// </summary>
        private static string _Ip = null;
        public static string IP
        {
            get { return _Ip; }
        }
        /// <summary>
        /// 
        /// </summary>
        private static uint _Port = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="data"></param>
        /// <param name="len"></param>
        private static void OnLogCb(int level, string data, int len)
        {
            //LogLevel eqLevel = level < 7 ? LogLevel.FromOrdinal(6 - level) : LogLevel.Off;

            ////_Logger.Log(eqLevel, data);
            //_Logger.Debug(data);
        }

        /** 20180208. Para que convivan mas de un proceso con la misma CORESIP */
        private static void CallbacksInit()
        {
            _Cb.OnLog = new LogCb((p1, p2, p3) =>
            {
                //if (Settings.Default.SipLogLevel <= 3)
                OnLog?.Invoke(p1, p2, p3);
            });
            _Cb.OnKaTimeout = new KaTimeoutCb((p1) =>
            {
                OnKaTimeout?.Invoke(p1);
            });
            _Cb.OnRdInfo = new RdInfoCb((p1, p2) =>
            {
                OnRdInfo?.Invoke(p1, p2);
            });
            _Cb.OnCallState = new CallStateCb((p1, p2, p3) =>
            {
                OnCallState?.Invoke(p1, p2, p3);
            });
            _Cb.OnCallIncoming = new CallIncomingCb((p1, p2, p3, p4) =>
            {
                OnCallIncoming?.Invoke(p1, p2, p3, p4);
            });
            _Cb.OnTransferRequest = new TransferRequestCb((p1, p2, p3) =>
            {
                OnTransferRequest?.Invoke(p1, p2, p3);
            });
            _Cb.OnTransferStatus = new TransferStatusCb((p1, p2) =>
            {
                OnTransferStatus?.Invoke(p1, p2);
            });
            _Cb.OnConfInfo = new ConfInfoCb((p1, p2) =>
            {
                OnConfInfo?.Invoke(p1, p2);
            });
            _Cb.OnOptionsReceive = new OptionsReceiveCb((p1) =>
            {
                OnOptionsReceive?.Invoke(p1);
            });
            _Cb.OnWG67Notify = new WG67NotifyCb((p1, p2, p3) =>
            {
                OnWG67Notify?.Invoke(p1, p2, p3);
            });
            _Cb.OnInfoReceived = new InfoReceivedCb((p1, p2, p3) =>
            {
                OnInfoReceived?.Invoke(p1, p2, p3);
            });
            //_Cb.OnIncomingSubscribeConf = new IncomingSubscribeConfCb((p1, p2, p3) =>
            //{
            //    if (OnIncomingSubscribeConf != null)
            //        OnIncomingSubscribeConf(p1, p2, p3);
            //});
            //_Cb.OnSubPres = new SubPresCb((p1, p2, p3) =>
            //{
            //    if (OnSubPres != null)
            //        OnSubPres(p1, p2, p3);
            //});
        }
        #endregion
    }
}
