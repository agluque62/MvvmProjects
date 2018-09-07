using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Threading;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using NuMvvmServices;

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
        private readonly IDlgService _dlgService;

        private BkkWebSocketServer _wss = null;
        private string _systemMessage;
        private ObservableCollection<WorkingUser> _uIUsers;
        private int _openSessions;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService, IDlgService dlgService)
        {
            _dataService = dataService;
            _dlgService = dlgService; 

            /** */
            _dataService.GetAppConfig((cfg, x) =>
            {
                Title = $"Simulador WS Brekeke. Nucleo 2018. Listening at {cfg.Ip}:{cfg.Port}...";

                _wss = new BkkWebSocketServer(cfg.Ip, cfg.Port);
                _wss.Start();
                _wss.Activate(_dataService);
            });

            IsStarted = true;
            NewUserName = "";
            OpenSessions = 0;

            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                _dlgService.Confirm("¿Desea Salir de la Aplicacion?", (res) =>
                {
                    if (res)
                    {
                        if (IsStarted)
                        {
                            _wss.Deactivate();
                            _wss.Stop();
                        }

                        _dataService.SaveWorkingUsers(null);
                        System.Windows.Application.Current.Shutdown();
                    }
                }, Title);
            });

            AppStartStop = new RelayCommand(() =>
            {
            });

            AppAddUser = new RelayCommand(() =>
            {
                if (NewUserName == string.Empty)
                {
                    Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Message) { Data = "El Nombre de Usuario no puede estar vacio"});
                }
                else if (_dataService.WorkingUserExist(NewUserName) == true)
                {
                    Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Message) { Data = $"Nombre de Usuario <{NewUserName}> repetido..."});
                }
                else
                {
                    _dlgService.Confirm($"¿Desea añadir el usuario {NewUserName}?", (res) =>
                    {
                        if (res)
                        {
                            WorkingUser newuser = new WorkingUser() { Name = NewUserName, /*Registered = true, */Status = UserStatus.Disconnect };
                            Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Register) { Data = newuser });
                        }
                    }, Title);
                }
            });

            /** Gestor de Mensajes... */
            Messenger.Default.Register<BkkSimEvent>(this, (ev) =>
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
                        _dataService.AddWorkingUser((ev.Data as WorkingUser).Name, null);
                        _wss.UpdateUser((ev.Data as WorkingUser).Name, true, false);
                        RaisePropertyChanged("UIUsers");
                        break;

                    case ModelEvents.Unregister:
                        _dlgService.Confirm($"¿Desea eliminar el usuario {(ev.Data as WorkingUser).Name}?", (res) =>
                        {
                            if (res)
                            {
                                _wss.InformUserUnregistered((ev.Data as WorkingUser).Name);
                                _dataService.DelWorkingUser((ev.Data as WorkingUser).Name, null);
                                RaisePropertyChanged("UIUsers");
                            }
                        }, Title);
                        break;

                    case ModelEvents.StatusChange:
                        _wss.UpdateUser((ev.Data as WorkingUser).Name, false, true);
                        break;

                    case ModelEvents.SessionOpen:
                        OpenSessions = OpenSessions + 1;
                        break;

                    case ModelEvents.SessionClose:
                        OpenSessions = OpenSessions - 1;
                        break;
                }
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
        public string NewUserName { get; set; }
        public string SystemMessage
        {
            get => _systemMessage;
            set
            {
                Set(ref _systemMessage, value);
            }
        }
        public ObservableCollection<WorkingUser> UIUsers
        {
            get
            {
                _dataService.GetWorkingUsers((users, x) =>
                {
                    _uIUsers = new ObservableCollection<WorkingUser>(users.Users);
                });
                return _uIUsers;
            }
            //set => _uIUsers = value;
        }
        public string Title { get; set; }
        public int OpenSessions
        {
            get => _openSessions;
            set
            {
                Set(ref _openSessions, value);
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