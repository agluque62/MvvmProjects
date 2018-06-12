using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using CoreSipNet;

namespace RadioVoipSimV2.Model 
{
    public class SipSession : ViewModelBase
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

        public SipSession(string user, bool isTx = false)
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
        public SimulatedFrequecy Freq { get => _freq; set => _freq = value; }

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
                return CallId!=-1 &( AircrafSquelch || ScvSquelch);
            }
            set
            {
                OnPropertyChanged("Squelch");
                if (Freq!=null) Freq.Squelch = value;
            }
        }
        public bool Ptt
        {
            get => _ptt;
            set {
                _ptt = value;
                OnPropertyChanged("Ptt");
                if (Freq != null) Freq.Ptt = value;
            }
        }
        public bool Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged("Error"); }
        }

        public bool AircrafSquelch
        {
            get => _aircrafSquelch;
            set { _aircrafSquelch = value; OnPropertyChanged("AircrafSquelch"); Squelch = value; }
        }
        public bool ScvSquelch
        {
            get => _scvSquelch;
            set { _scvSquelch = value; OnPropertyChanged("ScvSquelch"); Squelch = value; }
        }

        public void Reset()
        {
            CallId = -1;
            State = CORESIP_CallState.CORESIP_CALL_STATE_DISCONNECTED;
            Ptt = false;
            Error = false;
            ScvSquelch = false;
        }
    }
}
