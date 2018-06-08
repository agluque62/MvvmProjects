using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;

namespace RadioVoipSimV2.ViewModel
{
    class ViewModelMain : ViewModelBase
    {
        public ViewModelMain()
        {
            _activo = _main;
            ButtonText1 = "To Config";

            ChangePage = new DelegateCommandBase((obj) =>
            {
                if (_activo is UCMainViewModel)
                {
                    ActiveVm = _config;
                    ButtonText1 = "To Main";
                    OnPropertyChanged("ButtonText1");
                }
                else if (_activo is UCConfigViewModel)
                {
                    /** */
                    ActiveVm = _main;
                    ButtonText1 = "To Config";
                    OnPropertyChanged("ButtonText1");
                }
            });

            Exit = new DelegateCommandBase((obj) =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }

        #region Propiedades
        ViewModelBase _activo = null;
        public ViewModelBase ActiveVm
        {
            get
            {
                return _activo;
            }
            set
            {
                _activo = value;
                OnPropertyChanged("ActiveVm");
            }
        }

        public string ButtonText1 { get; set; }
        #endregion Propiedades

        #region Comandos

        public DelegateCommandBase ChangePage { get; set; }
        public DelegateCommandBase Exit { get; set; }

        #endregion Comandos

        #region Privados
        ViewModelBase _main = new UCMainViewModel();
        ViewModelBase _config = new UCConfigViewModel();
        #endregion
    }
}
