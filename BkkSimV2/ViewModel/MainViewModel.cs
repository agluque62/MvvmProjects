using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using BkkSimV2.Model;
using BkkSimV2.Services;

namespace BkkSimV2.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        private BkkWebSocketServer _wss = null;
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;

            /** */
            _dataService.GetAppConfig((cfg, x) =>
            {
                _wss = new BkkWebSocketServer(_dataService, cfg.Ip, cfg.Port);
            });

            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }
                });

            IsStarted = false;

            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                /** TODO. Llamar a Dispose */
                if (IsStarted) _wss.Stop();
                System.Windows.Application.Current.Shutdown();
            });

            AppStartStop = new RelayCommand(() =>
            {
                if (IsStarted) _wss.Stop();
                else _wss.Start();

                IsStarted = !IsStarted;
                RaisePropertyChanged("IsStarted");
            });

            AppAddUser = new RelayCommand(() =>
            {
                /** TODO */
            });

        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
        ///

        #region Propiedades para UI

            public bool IsStarted { get; set; }
            
        #endregion

        #region Comandos para UI
            public RelayCommand AppExit { get; set; }
            public RelayCommand AppStartStop { get; set; }
            public RelayCommand AppAddUser { get; set; }
        #endregion
    }
}