using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using NuMvvmServices;

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
        private readonly IDlgService _dlgService;
        private readonly ILogService _log;
        //private DataConfig dataConfig = null;
        private string _systemMessage;

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
        public MainViewModel(IDataService dataService, IDlgService dlgService, ISipPresenceService sipPresenceService)
        {
            _dataService = dataService;
            _dlgService = dlgService;
            _sipPresenceService = sipPresenceService;
            _log = new LogService();

            _sipPresenceService.OptionsReceived += (from) =>
            {
                _log.From().Info($"Options received from {from}");
            };

            _sipPresenceService.SubscribeReceived += (from) =>
            {
                RaisePropertyChanged("UIUsers");
                _log.From().Info($"Subscribe received from {from}");
            };

            _sipPresenceService.Configure(_dataService, (data, err) =>
            {
                if (err != null)
                {
                    WelcomeTitle = err.Message;
                    return;
                }
                //dataConfig = data;

                _sipPresenceService.Start((err1) =>
                {
                    if (err1 != null)
                    {
                        WelcomeTitle = err.Message;
                    }
                    else
                    {
                         WelcomeTitle = $"SimPresenciaSip. Nucleo 2018. Listen at {data.ListenIp}:{data.ListenPort}";
                    }
                });
            });


            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                _dlgService.Confirm("¿Desea Salir de la Aplicacion?", (res) =>
                {
                    if (res)
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }, WelcomeTitle);
            });

            AppStartStop = new RelayCommand(() =>
            {
            });

            AppAddUser = new RelayCommand(() =>
            {
                if (NewUserName == string.Empty)
                {
                    Messenger.Default.Send<ModelEvent>(new ModelEvent(ModelEvents.Message) { Data = "El Nombre de Usuario no puede estar vacio" });
                }
                else if (_dataService.UserExist(NewUserName) == true)
                {
                    Messenger.Default.Send<ModelEvent>(new ModelEvent(ModelEvents.Message) { Data = $"Nombre de Usuario <{NewUserName}> repetido..." });
                }
                else
                {
                    _dlgService.Confirm($"¿Desea añadir el usuario {NewUserName}?", (res) =>
                    {
                        if (res)
                        {
                            UserInfo newuser = new UserInfo() { Id = NewUserName, Status = "" };
                            Messenger.Default.Send<ModelEvent>(new ModelEvent(ModelEvents.Register) { Data = newuser });
                        }
                    }, WelcomeTitle);
                }
            });

            /** Gestor de Mensajes... */
            Messenger.Default.Register<ModelEvent>(this, (ev) =>
            {
                switch (ev.Event)
                {
                    case ModelEvents.Message:
                        Task.Factory.StartNew(() =>
                        {
                            var msg = ev.Data as string;
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                            {
                                SystemMessage = msg;
                                Task.Delay(5000).Wait();
                                SystemMessage = "";
                            });
                        });
                        break;

                    case ModelEvents.Register:
                        _sipPresenceService.AddUser((ev.Data as UserInfo).Id);
                        RaisePropertyChanged("UIUsers");
                        break;

                    case ModelEvents.Unregister:
                        _dlgService.Confirm($"¿Desea eliminar el usuario {(ev.Data as UserInfo).Id}?", (res) =>
                        {
                            if (res)
                            {
                                _sipPresenceService.RemoveUser((ev.Data as UserInfo).Id);
                                RaisePropertyChanged("UIUsers");
                            }
                        }, WelcomeTitle);
                        break;

                    case ModelEvents.StatusChange:
                        break;

                    case ModelEvents.SessionOpen:
                        break;

                    case ModelEvents.SessionClose:
                        break;
                }
            });

        }

        public override void Cleanup()
        {
            _log.From().Info("Saliendo de aplicacion...");
            _dataService.SaveData(null);
            // Clean up if needed
            _sipPresenceService.Stop(null);
            base.Cleanup();
        }

        #region Propiedades para presentacion
        private ObservableCollection<UserInfo> _uIUsers;
        public ObservableCollection<UserInfo> UIUsers
        {
            get
            {
                _dataService.GetData((data, x) => 
                {
                    if (x == null)
                        _uIUsers = new ObservableCollection<UserInfo>(data.LastUsers);
                    foreach (var user in _uIUsers)
                    {
                        user.Status = _sipPresenceService.SubscriptionsTo(user.Id).ToString();
                    }
                });
                return _uIUsers;
            }
        }

        public string NewUserName { get; set; }
        public string SystemMessage
        {
            get => _systemMessage;
            set
            {
                Set(ref _systemMessage, value);
            }
        }

        #endregion

        #region Comandos para UI
        public RelayCommand AppExit { get; set; }
        public RelayCommand AppStartStop { get; set; }
        public RelayCommand AppAddUser { get; set; }
        #endregion
    }
}