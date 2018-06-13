using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using RadioVoipSimV2.Model;

namespace RadioVoipSimV2.ViewModel
{
    class UCConfigViewModel : RadioVoipSimV2.MvvmFramework.ViewModelBase
    {
        private AppConfig _config;
        private AppConfig.FrequencyConfig _selectedFreq;
        private int _selectedTxIndex;
        private int _selectedRxIndex;

        public UCConfigViewModel()
        {
            Title = String.Format("Simulador de Equipos Radio Voip. Nucleo 2018. [Configurador]");
            AppConfig.GetAppConfig((cfg, ex) =>
            {
                Config = cfg;
                if (Config.SimulatedFrequencies.Count > 0)
                {
                    SelectedFreq = Config.SimulatedFrequencies[0];
                    if (SelectedFreq.RxUsers.Count > 0)
                        SelectedRxIndex = 0;
                    if (SelectedFreq.TxUsers.Count > 0)
                        SelectedTxIndex = 0;
                }
            });

            AddFreq = new DelegateCommandBase((obj) =>
            {
                Config.SimulatedFrequencies.Add(new AppConfig.FrequencyConfig()
                {
                    Id = "100.000",
                    RxUsers = new List<string>(),
                    TxUsers = new List<string>()
                });
                OnPropertyChanged("Config");
            });

            DelFreq = new DelegateCommandBase((obj) =>
            {
                if (obj is Int32 index && index >=0)
                {
                    // if (_dialogService.Confirm("¿Desea eliminar el item seleccionado?"))
                    {
                        Config.SimulatedFrequencies.RemoveAt(index);
                        OnPropertyChanged("Config");
                    }
                }
            });

            AddEquipmentTx = new DelegateCommandBase((par) =>
            {
                if (SelectedFreq != null)
                {
                    SelectedFreq.TxUsers.Add("TXNEW");
                    OnPropertyChanged("Config");
                }
            });

            DelEquipmentTx = new DelegateCommandBase((par) =>
            {
                if (par is Int32 index && index >= 0 && SelectedFreq != null)
                {
                    SelectedFreq.TxUsers.RemoveAt(index);
                    OnPropertyChanged("Config");
                }
            });

            AddEquipmentRx = new DelegateCommandBase((par) =>
            {
                if (SelectedFreq != null)
                {
                    SelectedFreq.RxUsers.Add("RXNEW");
                    OnPropertyChanged("Config");
                }
            });

            DelEquipmentRx = new DelegateCommandBase((par) =>
            {
                if (par is Int32 index && index >= 0 && SelectedFreq != null)
                {
                    SelectedFreq.RxUsers.RemoveAt(index);
                    OnPropertyChanged("Config");
                }
            });
        }

        public void Dispose()
        {
        }

        public AppConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged("Config");
            }
        }

        public AppConfig.FrequencyConfig SelectedFreq { get => _selectedFreq; set => _selectedFreq = value; }
        public int SelectedTxIndex { get => _selectedTxIndex; set => _selectedTxIndex = value; }
        public int SelectedRxIndex { get => _selectedRxIndex; set => _selectedRxIndex = value; }

        public DelegateCommandBase AddFreq { get; set; }
        public DelegateCommandBase DelFreq { get; set; }
        public DelegateCommandBase AddEquipmentRx { get; set; }
        public DelegateCommandBase DelEquipmentRx { get; set; }
        public DelegateCommandBase AddEquipmentTx { get; set; }
        public DelegateCommandBase DelEquipmentTx { get; set; }

    }
}
