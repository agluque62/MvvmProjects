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
            _activo = new UCMainViewModel();
            //_activo = new UCConfigViewModel();

            Btn01Command = new DelegateCommandBase((obj) =>
            {
                if (_activo is UCMainViewModel)
                {
                    (_activo as UCMainViewModel).Dispose();
                    ActiveVm = new UCConfigViewModel();
                }
                else if (_activo is UCConfigViewModel)
                {
                    (_activo as UCConfigViewModel).Save();
                    ActiveVm = new UCMainViewModel();
                }
            });

            Btn02Command = new DelegateCommandBase((obj) =>
            {
                if (_activo is UCMainViewModel)
                {
                    (_activo as UCMainViewModel).Dispose();
                    System.Windows.Application.Current.Shutdown();
                }
                else if (_activo is UCConfigViewModel)
                {
                    (_activo as UCConfigViewModel).Cancel();
                    ActiveVm = new UCMainViewModel();
                }
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

        public DelegateCommandBase Btn01Command { get; set; }
        public DelegateCommandBase Btn02Command { get; set; }

        #endregion Comandos

        #region Privados
        #endregion
    }
}
