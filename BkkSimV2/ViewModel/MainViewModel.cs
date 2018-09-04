using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;

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
        private string _systemMessage;
        private ObservableCollection<WorkingUser> _uIUsers;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;

            /** */
            _dataService.GetAppConfig((cfg, x) =>
            {
                _wss = new BkkWebSocketServer(cfg.Ip, cfg.Port);
                _wss.Start();
                _wss.Activate(_dataService);
            });

            IsStarted = true;
            NewUserName = "";

            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                if (IsStarted)
                {
                    _wss.Deactivate();
                    _wss.Stop();
                }

                _dataService.SaveWorkingUsers(null);
                System.Windows.Application.Current.Shutdown();
            });

            AppStartStop = new RelayCommand(() =>
            {
            });

            AppAddUser = new RelayCommand(() =>
            {
                if (NewUserName == string.Empty)
                {
                    Messenger.Default.Send<string>("El Nombre de Usuario no puede estar vacio");
                }
                else if (_dataService.WorkingUserExist(NewUserName) == true)
                {
                    Messenger.Default.Send<string>($"Nombre de Usuario <{NewUserName}> repetido...");
                }
                else
                {
                    //if (dlgService.ShowQuestion($"¿Desea añadir el usuario {NewUserName}?", "BkkSimV2") == true)
                    {
                        WorkingUser newuser = new WorkingUser() { Name = NewUserName, /*Registered = true, */Status = UserStatus.Available };
                        Messenger.Default.Send<WorkingUserEvent>(new WorkingUserEvent() { User = newuser, Event = ModelEvents.Register });
                    }
                }
            });

            /** Mensaje que se envia como evento de usarios usuario */
            Messenger.Default.Register<WorkingUserEvent>(this, (ev) =>
            {
                switch (ev.Event)
                {
                    case ModelEvents.Register:
                        _dataService.AddWorkingUser(ev.User.Name, null);
                        _wss.UpdateUser(ev.User.Name, true, false);
                        RaisePropertyChanged("UIUsers");
                        break;

                    case ModelEvents.Unregister:
                        _wss.InformUserUnregistered(ev.User.Name);
                        _dataService.DelWorkingUser(ev.User.Name, null);
                        RaisePropertyChanged("UIUsers");
                        break;

                    case ModelEvents.StatusChange:
                        _wss.UpdateUser(ev.User.Name, false, true);
                        break;
                }
            });

            /** Para enviar mensajes a la línea de mensajes */
            Messenger.Default.Register<String>(this, (msg) =>
            {
                Task.Factory.StartNew(() =>
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        SystemMessage = msg;
                        Task.Delay(5000).Wait();
                        SystemMessage = "";
                    });
                });
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
        #endregion

        #region Comandos para UI
        public RelayCommand AppExit { get; set; }
        public RelayCommand AppStartStop { get; set; }
        public RelayCommand AppAddUser { get; set; }
        #endregion
    }
}