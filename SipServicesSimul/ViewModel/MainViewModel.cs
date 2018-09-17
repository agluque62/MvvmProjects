using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
                         WelcomeTitle = $"Simulador Servidor Sip Presencia. Nucleo 2018. Listen at {data.ListenIp}:{data.ListenPort}";
                    }
                });
            });


            /** Programacion de los comandos UI */
            AppExit = new RelayCommand(() =>
            {
                //_dlgService.Confirm("¿Desea Salir de la Aplicacion?", (res) =>
                //{
                //    if (res)
                //    {
                //        if (IsStarted)
                //        {
                //            _wss.Deactivate();
                //            _wss.Stop();
                //        }

                //        _dataService.SaveWorkingUsers(null);
                //        System.Windows.Application.Current.Shutdown();
                //    }
                //}, Title);
            });

            AppStartStop = new RelayCommand(() =>
            {
            });

            AppAddUser = new RelayCommand(() =>
            {
                //if (NewUserName == string.Empty)
                //{
                //    Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Message) { Data = "El Nombre de Usuario no puede estar vacio" });
                //}
                //else if (_dataService.WorkingUserExist(NewUserName) == true)
                //{
                //    Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Message) { Data = $"Nombre de Usuario <{NewUserName}> repetido..." });
                //}
                //else
                //{
                //    _dlgService.Confirm($"¿Desea añadir el usuario {NewUserName}?", (res) =>
                //    {
                //        if (res)
                //        {
                //            WorkingUser newuser = new WorkingUser() { Name = NewUserName, /*Registered = true, */Status = UserStatus.Disconnect };
                //            Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Register) { Data = newuser });
                //        }
                //    }, Title);
                //}
            });

            /** Gestor de Mensajes... */
            Messenger.Default.Register<string>(this, (ev) =>
            //Messenger.Default.Register<BkkSimEvent>(this, (ev) =>
            {
                //switch (ev.Event)
                //{
                //    case ModelEvents.Message:
                //        Task.Factory.StartNew(() =>
                //        {
                //            var msg = ev.Data as string;
                //            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                //            {
                //                SystemMessage = msg;
                //                Task.Delay(5000).Wait();
                //                SystemMessage = "";
                //            });
                //        });
                //        break;

                //    case ModelEvents.Register:
                //        _dataService.AddWorkingUser((ev.Data as WorkingUser).Name, null);
                //        _wss.UpdateUser((ev.Data as WorkingUser).Name, true, false);
                //        RaisePropertyChanged("UIUsers");
                //        break;

                //    case ModelEvents.Unregister:
                //        _dlgService.Confirm($"¿Desea eliminar el usuario {(ev.Data as WorkingUser).Name}?", (res) =>
                //        {
                //            if (res)
                //            {
                //                _wss.InformUserUnregistered((ev.Data as WorkingUser).Name);
                //                _dataService.DelWorkingUser((ev.Data as WorkingUser).Name, null);
                //                RaisePropertyChanged("UIUsers");
                //            }
                //        }, Title);
                //        break;

                //    case ModelEvents.StatusChange:
                //        _wss.UpdateUser((ev.Data as WorkingUser).Name, false, true);
                //        break;

                //    case ModelEvents.SessionOpen:
                //        OpenSessions = OpenSessions + 1;
                //        break;

                //    case ModelEvents.SessionClose:
                //        OpenSessions = OpenSessions - 1;
                //        break;
                //}
            });

        }

        public override void Cleanup()
        {
            _dataService.SaveData(dataConfig, null);
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
                        user.Status = "1";
                    }
                });
                return _uIUsers;
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