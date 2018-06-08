using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioVoipSimV2.Model
{
    public class SimulatedFrequecy
    {
        private string _name;
        private bool _ptt;
        private bool _squelch;

        public string Name { get => _name; set => _name = value; }
        public bool Ptt { get => _ptt; set => _ptt = value; }
        public bool Squelch { get => _squelch; set => _squelch = value; }
        public ObservableCollection<SipSesion> Sessions { get; set; }
    }
}
