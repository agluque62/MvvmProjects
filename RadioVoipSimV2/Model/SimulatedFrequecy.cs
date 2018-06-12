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
        private /*ObservableCollection*/List<SipSession> _sessions;
        private AppConfig.FrequencyConfig _config;
        public  AppConfig.FrequencyConfig Config { get => _config; set => _config = value; }

        public bool Ptt
        {
            get
            {
                var inPtt = _sessions.Where(s => s.IsTx && s.Ptt).ToList();
                return inPtt.Count > 0;
            }
            set
            {
                OnPropertyChanged("Ptt");
            }
        }
        public bool Squelch
        {
            get
            {
                var inSqh = _sessions.Where(s => s.IsTx == false && s.Squelch).ToList();
                return inSqh.Count > 0;
            }
            set
            {
                OnPropertyChanged("Squelch");
            }
        }
        public /*ObservableCollection*/List<SipSession> Sessions { get => _sessions; set => _sessions = value; }
        public bool IsActive { get; set; }
    }
}
