using GalaSoft.MvvmLight;
using SipServicesSimul.Model;

using SipServicesSimul.Services;

namespace SipServicesSimul.ViewModel
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
        private readonly ISipPresenceService _sipPresenceService;
        private DataConfig dataConfig = null;

        /// <summary>
        /// The <see cref="WelcomeTitle" /> property's name.
        /// </summary>
        public const string WelcomeTitlePropertyName = "WelcomeTitle";

        private string _welcomeTitle = string.Empty;

        /// <summary>
        /// Gets the WelcomeTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }
            set
            {
                Set(ref _welcomeTitle, value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService, ISipPresenceService sipPresenceService)
        {
            _dataService = dataService;
            _sipPresenceService = sipPresenceService;

            _sipPresenceService.Configure(_dataService, (data, err) =>
            {
                if (err != null)
                {
                    WelcomeTitle = err.Message;
                    return;
                }
                dataConfig = data;

                _sipPresenceService.Start((err1) =>
                {
                    if (err1 != null)
                    {
                        WelcomeTitle = err.Message;
                    }
                    else
                    {
                         WelcomeTitle = $"Listen on {data.ListenIp}:{data.ListenPort}";
                    }
                });

            });

        }

        public override void Cleanup()
        {
            // Clean up if needed
            _sipPresenceService.Stop(null);
            base.Cleanup();
        }
    }
}