using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Uv5kiNbxSimV2.Model;

namespace Uv5kiNbxSimV2.ViewModel
{
    public class MainUserControlViewModel : ViewModelBase, IDisposable
    {
        public MainUserControlViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _uiContext = System.Threading.SynchronizationContext.Current;

            _dataService.GetAppData((cfg, error) =>
            {
                if (error != null)
                {
                    // Report error here
                    return;
                }

                UlisesNbxItem.NotifyChange += (msg) =>
                {
                    _uiContext.Send(x =>
                    {
                        if (_mensajes.Count >= 4)
                        {
                            _mensajes.RemoveAt(3);
                        }
                        _mensajes.Insert(0, new LogMessage() { Msg = DateTime.Now.ToLongTimeString() + ": " + msg });
                        RaisePropertyChanged("Mensajes");
                    }, null);
                };

                _mensajes.Insert(0, new LogMessage() { Msg = DateTime.Now.ToLongTimeString() + ": Mensaje Inicial..." });

                UlisesNbxItem.ServerIp = cfg.ServerIP;
                UlisesNbxItem.ServerPort = cfg.ServerPort; 

                foreach(var nbx in cfg.Nbxs)
                {
                    var item = new UlisesNbxItem() { Ip = nbx.Ip, WebPort = nbx.RadioWp };
                    item.Start();
                    Nbxs.Add(item);
                };

                ListClean = new RelayCommand(() =>
                {
                    _mensajes.Clear();
                    RaisePropertyChanged("Mensajes");
                });

            });
        }

        public void Dispose()
        {
            Nbxs.ForEach(nbx =>
            {
                nbx.Dispose();
            });
            Nbxs.Clear();
        }

        private readonly IDataService _dataService;
        System.Threading.SynchronizationContext _uiContext = null;
        private ObservableCollection<LogMessage> _mensajes = new ObservableCollection<LogMessage>();
        public ObservableCollection<LogMessage> Mensajes
        {
            get
            {
                return _mensajes;
            }
            set
            {
                Set(ref _mensajes, value);
            }
        }
        private List<UlisesNbxItem> _nbxs = new List<UlisesNbxItem>();
        public List<UlisesNbxItem> Nbxs { get { return _nbxs; } }

        public RelayCommand ListClean { get; set; }

    }
}
