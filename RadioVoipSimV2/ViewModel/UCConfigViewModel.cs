using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<AppConfig> _listOfConfig;

        public UCConfigViewModel()
        {
            Title = String.Format("Simulador de Equipos Radio Voip. Nucleo 2018. [Configurador]");
            Btn01Text = "Aceptar";
            Btn02Text = "Cancelar";

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
                ListOfConfig = new List<AppConfig>() { cfg };
            });

            AddFreq = new DelegateCommandBase((obj) =>
            {
                Config.SimulatedFrequencies.Add(new AppConfig.FrequencyConfig()
                {
                    Id = "100.000",
                    RxUsers = new ObservableCollection<AppConfig.EquipmentConfig>(),
                    TxUsers = new ObservableCollection<AppConfig.EquipmentConfig>()
                });
                OnPropertyChanged("Config");
            });

            DelFreq = new DelegateCommandBase((obj) =>
            {
                if (obj is Int32 index && index >= 0)
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
                    SelectedFreq.TxUsers.Add(new AppConfig.EquipmentConfig() { Id = "TXNEW" });
                    OnPropertyChanged("SelectedFreq");
                }
            });

            DelEquipmentTx = new DelegateCommandBase((par) =>
            {
                if (par is Int32 index && index >= 0 && SelectedFreq != null)
                {
                    SelectedFreq.TxUsers.RemoveAt(index);
                    OnPropertyChanged("SelectedFreq");
                }
            });

            AddEquipmentRx = new DelegateCommandBase((par) =>
            {
                if (SelectedFreq != null)
                {
                    SelectedFreq.RxUsers.Add(new AppConfig.EquipmentConfig() { Id = "RXNEW" });
                    OnPropertyChanged("SelectedFreq");
                }
            });

            DelEquipmentRx = new DelegateCommandBase((par) =>
            {
                if (par is Int32 index && index >= 0 && SelectedFreq != null)
                {
                    SelectedFreq.RxUsers.RemoveAt(index);
                    OnPropertyChanged("SelectedFreq");
                }
            });
        }

        public void Save()
        {
            AppConfig.SetAppConfig(Config);
        }

        public void Cancel()
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

        public AppConfig.FrequencyConfig SelectedFreq
        {
            get => _selectedFreq;
            set
            {
                _selectedFreq = value;
                OnPropertyChanged("SelectedFreq");
            }
        }
        public int SelectedTxIndex { get => _selectedTxIndex; set => _selectedTxIndex = value; }
        public int SelectedRxIndex { get => _selectedRxIndex; set => _selectedRxIndex = value; }
        public List<AppConfig> ListOfConfig { get => _listOfConfig; set => _listOfConfig = value; }

        public DelegateCommandBase AddFreq { get; set; }
        public DelegateCommandBase DelFreq { get; set; }
        public DelegateCommandBase AddEquipmentRx { get; set; }
        public DelegateCommandBase DelEquipmentRx { get; set; }
        public DelegateCommandBase AddEquipmentTx { get; set; }
        public DelegateCommandBase DelEquipmentTx { get; set; }

    }
}
