using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Uv5kiNbxSimV2.Model;
using Uv5kiNbxSimV2.Services;

namespace Uv5kiNbxSimV2.ViewModel
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
        private readonly IDialogService _dialogService;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService, IDialogService dialogService)
        {
            CurrentViewModel = new MainUserControlViewModel(dataService);

            _dataService = dataService;
            _dialogService = dialogService;

            AppExit = new RelayCommand(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });

            AppConfig = new RelayCommand(() =>
            {
                if (CurrentViewModel is MainUserControlViewModel)
                {
                    (CurrentViewModel as MainUserControlViewModel).Dispose();
                    //CurrentViewModel = ViewModelLocator.ConfigModel;
                    CurrentViewModel = new ConfigUserControlViewModel(_dataService, _dialogService);
                }
            });

            AppMain = new RelayCommand(() =>
            {
                if (CurrentViewModel is ConfigUserControlViewModel)
                {
                    (CurrentViewModel as ConfigUserControlViewModel).Dispose();
                    //CurrentViewModel = ViewModelLocator.MainModel;
                    CurrentViewModel = new MainUserControlViewModel(_dataService);
                }
            });

        }

        ViewModelBase _current;
        public ViewModelBase CurrentViewModel
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
                RaisePropertyChanged("CurrentViewModel");
            }
        }

        public RelayCommand AppExit { get; set; }
        public RelayCommand AppConfig { get; set; }
        public RelayCommand AppMain { get; set; }


        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}