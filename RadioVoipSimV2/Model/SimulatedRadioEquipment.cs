using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using CoreSipNet;

namespace RadioVoipSimV2.Model 
{
    public enum RemoteControlStatusValues { NotPresent=3, Ok=1, Fail=2, Local=0 }
    public class SimulatedRadioEquipment : ViewModelBase
    {
        private string _name;
        private bool _isTx;
        private bool _habilitado;
        private int _callId;
        private CORESIP_CallState _state;
        private bool _ptt;
        private bool _error;
        private bool _aircrafSquelch;
        private bool _scvSquelch;
        private SimulatedFrequecy _freq;
        private int _squWavPlayer;
        private string _tuneIn;
        private RemoteControlStatusValues _remoteControlStatus;
        private string _remoteControlExtendedData;

        public SimulatedRadioEquipment(string user, bool isTx = false)
        {
            Name = user;
            IsTx = isTx;
            Habilitado = true;
            AircrafSquelch = false;

            Reset();
        }
        public string Name { get => _name; set => _name = value; }
        public bool IsTx { get => _isTx; set => _isTx = value; }
        public bool Habilitado
        {
            get => _habilitado;
            set
            {
                _habilitado = value;
                OnPropertyChanged("Habilitado");
            }
        }
        public SimulatedFrequecy FreqObject { get => _freq; set => _freq = value; }
        public int CallId { get => _callId; set => _callId = value; }
        public CORESIP_CallState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged("State");
            }
        }
        public bool Squelch
        {
            get
            {
                return CallId != -1 & (AircrafSquelch || ScvSquelch);
            }
            set
            {
                if (FreqObject != null) FreqObject.Squelch = value;
                OnPropertyChanged("Squelch");
            }
        }
        public bool Ptt
        {
            get => _ptt;
            set
            {
                _ptt = value;
                OnPropertyChanged("Ptt");
                if (FreqObject != null) FreqObject.Ptt = value;
            }
        }
        public bool Error
        {
            get => _error;
            set
            {
                _error = value;
                OnPropertyChanged("Error");
                if (FreqObject != null) FreqObject.Status = FrequencyStatus.Error;
            }
        }
        public bool AircrafSquelch
        {
            get => _aircrafSquelch;
            set { _aircrafSquelch = value; OnPropertyChanged("AircrafSquelch"); Squelch = value; }
        }
        //public bool AircrafSquelch
        //{
        //    get
        //    {
        //        return FreqObject != null ? FreqObject.AircrafSquelch : false;
        //    }
        //    set { /*_aircrafSquelch = value; */OnPropertyChanged("AircrafSquelch"); /*Squelch = value;*/ }
        //}
        public bool ScvSquelch
        {
            get => _scvSquelch;
            set { _scvSquelch = value; OnPropertyChanged("ScvSquelch"); Squelch = value; }
        }
        public int LocalSessionAudio
        {
            get => _squWavPlayer;
            set
            {
                _squWavPlayer = value;
            }
        }

        public string Band
        {
            get
            {
                return (Config is AppConfig.MainEquipmentConfig) ? FreqObject?.Config.Band :
                    (Config as AppConfig.StandbyEquipmentConfig).Band;
            }
        }
        public string TuneIn
        {
            get => _tuneIn;
            set
            {
                _tuneIn = value;
                OnPropertyChanged("NameAndTuneIn");
            }
        }
        public string NameAndTuneIn { get => String.Format("{0} ({1})", Name, _tuneIn); }
        public EquipmentConfig Config { get; set; }
        public void Reset()
        {
            CallId = -1;
            State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            Ptt = false;
            Error = false;
            ScvSquelch = false;
        }

        /** Control SNMP del equipo */
        public event Action<string, RemoteControlStatusValues> NotifyRemoteControlChange;
        public RemoteControlStatusValues RemoteControlStatus
        {
            get => _remoteControlStatus;
            set
            {
                _remoteControlStatus = value;
                NotifyRemoteControlChange?.Invoke(Name, _remoteControlStatus);
                OnPropertyChanged("RemoteControlStatus");
            }
        }
        public IList<RemoteControlStatusValues> RemoteControlStatusStrings
        {
            get
            {
                return Enum.GetValues(typeof(RemoteControlStatusValues)).Cast<RemoteControlStatusValues>().ToList<RemoteControlStatusValues>();
            }
        }
        public string RemoteControlExtendedData
        {
            get => _remoteControlExtendedData;
            set
            {
                _remoteControlExtendedData = value;
                OnPropertyChanged("RemoteControlExtendedData");
            }
        }
    }
}
