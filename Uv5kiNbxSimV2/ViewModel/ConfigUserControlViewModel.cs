using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Uv5kiNbxSimV2.Model;
using Uv5kiNbxSimV2.Services;

namespace Uv5kiNbxSimV2.ViewModel
{
    class ConfigUserControlViewModel : ViewModelBase, IDisposable
    {
        public ConfigUserControlViewModel(IDataService dataService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            _dataService = dataService;
            _dataService.GetAppData((cfg, error) =>
            {
                JData = cfg;
            });

            CfgAdd = new RelayCommand(() => {
                _config.Nbxs.Add(new AppDataConfig.JSonNbxConfig() { Ip = "127.0.0.1", Wp = 8000 });
                RaisePropertyChanged("JData");
            });

            CfgDel = new RelayCommand(() => {
                Int32 iborrar = SelectedIndex;
                if (iborrar >= 0)
                {
                    if (_dialogService.Confirm("¿Desea eliminar el item seleccionado?") )
                    {
                        _config.Nbxs.RemoveAt(iborrar);
                        RaisePropertyChanged("JData");
                    }
                }
            });
        }

        public void Dispose()
        {
            _dataService.SetAppData(JData);
        }

        private readonly IDataService _dataService;
        private readonly IDialogService _dialogService;

        private AppDataConfig.JSonConfig _config = null;
        public AppDataConfig.JSonConfig JData
        {
            get
            {
                return _config;
            }
            set
            {
                Set(ref _config, value);
            }
        }

        public AppDataConfig.JSonNbxConfig SelectedItem { get; set; }
        public Int32 SelectedIndex { get; set; }

        public RelayCommand CfgDel { get; set; }
        public RelayCommand CfgAdd { get; set; }

    }
}
