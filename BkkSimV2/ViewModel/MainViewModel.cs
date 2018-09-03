using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Threading;
using System.Threading.Tasks;

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
                _wss = new BkkWebSocketServer(_dataService, cfg.Ip, cfg.Port);
            });

            IsStarted = false;
            NewUserName = "";
            SystemMessage = "Hola Hola..";

            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                /** TODO. Llamar a Dispose */
                if (IsStarted) _wss.Stop();
                _dataService.SaveWorkingUsers(null);
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
                if (NewUserName == string.Empty)
                {
                    SendSystemMessage("El Nombre de Usuario no puede estar vacio");
                }
                else if (_dataService.WorkingUserExist(NewUserName) == true)
                {
                    SendSystemMessage($"Nombre de Usuario <{NewUserName}> repetido...");
                }
                else
                {
                    Messenger.Default.Send<WorkingUser>(new WorkingUser() { Name = NewUserName, /*Registered = true, */Status = UserStatus.Available });
                }
            });

            /** Mensaje que se envia al añadir un nuevo usuario */
            Messenger.Default.Register<WorkingUser>(this, (newuser) =>
            {
                _dataService.AddWorkingUser(newuser.Name, null);
                _wss.UpdateUser(newuser.Name);
                RaisePropertyChanged("UIUsers");
            });

        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
        ///

        protected void SendSystemMessage(string msg)
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
        }

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