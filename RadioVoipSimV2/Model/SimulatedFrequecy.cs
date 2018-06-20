using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;

namespace RadioVoipSimV2.Model
{
    public enum FrequencyStatus { NotOperational, Operational, Degraded, Error }
    public class SimulatedFrequecy : ViewModelBase
    {
        private /*ObservableCollection*/List<SimulatedRadioEquipment> _sessions;
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
        public /*ObservableCollection*/List<SimulatedRadioEquipment> Equipments { get => _sessions; set => _sessions = value; }
        public FrequencyStatus Status
        {
            get
            {
                int ConnectedTxs = Equipments.Where(t => t.IsTx && t.CallId != -1).ToList().Count();
                int ConnectedRxs = Equipments.Where(t => t.IsTx==false && t.CallId != -1).ToList().Count();
                int InError = Equipments.Where(s => s.Error).ToList().Count;

                return (ConnectedTxs == 0 && ConnectedRxs == 0) ? FrequencyStatus.NotOperational :
                    (InError > 0) ? FrequencyStatus.Error :
                    (ConnectedRxs != ConnectedTxs) ? FrequencyStatus.Degraded : FrequencyStatus.Operational;

            }
            set
            {
                OnPropertyChanged("Status");
            }
        }
        
    }
}
