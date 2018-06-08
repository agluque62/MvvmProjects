using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreSipNet;

namespace RadioVoipSimV2.Model
{
    public class SipSesion
    {
        private string _name;
        private bool _isTx;
        private bool _habilitado;
        private int _callId;
        private CORESIP_CallState _state;
        private CORESIP_CallInfo _info;
        private bool _squelch;
        private bool _ptt;

        public string Name { get => _name; set => _name = value; }
        public bool IsTx { get => _isTx; set => _isTx = value; }
        public bool Habilitado { get => _habilitado; set => _habilitado = value; }
        public int CallId { get => _callId; set => _callId = value; }
        public CORESIP_CallState State { get => _state; set => _state = value; }
        public CORESIP_CallInfo Info { get => _info; set => _info = value; }
        public bool Squelch { get => _squelch; set => _squelch = value; }
        public bool Ptt { get => _ptt; set => _ptt = value; }
        public bool Error { get; set; }
    }
}
