using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;

namespace RadioVoipSimV2.Model
{
    public class SimulatedFrequecy : ViewModelBase
    {
        private bool _ptt;
        private bool _squelch;
        private ObservableCollection<SipSesion> _sessions;
        private AppConfig.FrequencyConfig _config;

        public AppConfig.FrequencyConfig Config { get => _config; set => _config = value; }

        public bool Ptt
        {
            get => _ptt;
            set
            {
                _ptt = value;
                OnPropertyChanged("Ptt");
            }
        }
        public bool Squelch
        {
            get => _squelch;
            set
            {
                _squelch = value;
                OnPropertyChanged("Squelch");
            }
        }
        public ObservableCollection<SipSesion> Sessions { get => _sessions; set => _sessions = value; }
        public bool IsActive { get; set; }
    }
}
