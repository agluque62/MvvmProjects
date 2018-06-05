using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Helpers;

namespace Uv5kiNbxSimV2.Services
{
    public interface IDialogService
    {
        bool Confirm(string message);
    }

    class DialogService : /*GalaSoft.MvvmLight.Views.*/IDialogService
    {
        #region GalaSoft.MvvmLight.Views.IDialogService
        public Task ShowError(string message, string title, string buttonText, Action afterHideCallback)
        {
            throw new NotImplementedException();
        }

        public Task ShowError(Exception error, string title, string buttonText, Action afterHideCallback)
        {
            throw new NotImplementedException();
        }

        public Task ShowMessage(string message, string title)
        {
            throw new NotImplementedException();
        }

        public Task ShowMessage(string message, string title, string buttonText, Action afterHideCallback)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ShowMessage(string message, string title, string buttonConfirmText, string buttonCancelText, Action<bool> afterHideCallback)
        {
            throw new NotImplementedException();
        }

        public Task ShowMessageBox(string message, string title)
        {
            throw new NotImplementedException();
        }
        #endregion GalaSoft.MvvmLight.Views.IDialogService

        public bool Confirm(string message)
        {
            MessageBoxResult res = MessageBox.Show(message, "", MessageBoxButton.YesNo);
            return res == MessageBoxResult.Yes ? true : false;
        }
    }
}
