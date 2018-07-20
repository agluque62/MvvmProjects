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
        private /*ObservableCollection*/List<SimulatedRadioEquipment> _mainEquipment;
        private AppConfig.FrequencyConfig _config;
        private bool _aircrafSquelch;
        private List<SimulatedRadioEquipment> _stanbyEquipments;

        public AppConfig.FrequencyConfig Config { get => _config; set => _config = value; }
        public bool Ptt
        {
            get
            {
                var inPtt = CurrentAssignedEquipments.Where(s => s.IsTx && s.Ptt).ToList();
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
                var inSqh = CurrentAssignedEquipments.Where(s => s.IsTx == false && s.Squelch).ToList();
                return inSqh.Count > 0;
            }
            set
            {
                OnPropertyChanged("Squelch");
            }
        }
        public /*ObservableCollection*/List<SimulatedRadioEquipment> MainEquipments { get => _mainEquipment; set => _mainEquipment = value; }
        public List<SimulatedRadioEquipment> StanbyEquipments { get => _stanbyEquipments; set => _stanbyEquipments = value; }

        public FrequencyStatus Status
        {
            get
            {
                int ConnectedTxs = CurrentAssignedEquipments.Where(t => t.IsTx && t.CallId != -1).ToList().Count();
                int ConnectedRxs = CurrentAssignedEquipments.Where(t => t.IsTx == false && t.CallId != -1).ToList().Count();
                int InError = CurrentAssignedEquipments.Where(s => s.Error).ToList().Count;

                return (ConnectedTxs == 0 && ConnectedRxs == 0) ? FrequencyStatus.NotOperational :
                    (InError > 0) ? FrequencyStatus.Error :
                    (ConnectedRxs != ConnectedTxs) ? FrequencyStatus.Degraded : FrequencyStatus.Operational;

            }
            set
            {
                OnPropertyChanged("Status");
            }
        }
        public bool AircrafSquelch
        {
            get => _aircrafSquelch;
            set { _aircrafSquelch = value; OnPropertyChanged("AircrafSquelch"); Squelch = value; }
        }

        private List<SimulatedRadioEquipment> CurrentAssignedEquipments
        {
            get
            {
                var equipments = new List<SimulatedRadioEquipment>(  MainEquipments);

                var stbyEquipments = StanbyEquipments.Where(e => e.FreqObject == this && e.CallId != -1).ToList();
                equipments.AddRange(stbyEquipments);
                return equipments;
            }
        }
    }
}
