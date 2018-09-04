using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;

using GalaSoft.MvvmLight.Views;
using GalaSoft.MvvmLight.Messaging;

namespace BkkSimV2.Services
{

    class DialogService : IDialogService
    {
        Task IDialogService.ShowError(string message, string title, string buttonText, Action afterHideCallback)
        {
            //MessageBox.Show(message, title);
            //afterHideCallback?.Invoke();
            //return null;
            throw new NotImplementedException();
        }

        Task IDialogService.ShowError(Exception error, string title, string buttonText, Action afterHideCallback)
        {
            throw new NotImplementedException();
        }

        Task IDialogService.ShowMessage(string message, string title)
        {
            throw new NotImplementedException();
        }

        Task IDialogService.ShowMessage(string message, string title, string buttonText, Action afterHideCallback)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDialogService.ShowMessage(string message, string title, string buttonConfirmText, string buttonCancelText, Action<bool> afterHideCallback)
        {
            throw new NotImplementedException();
        }

        Task IDialogService.ShowMessageBox(string message, string title)
        {
            throw new NotImplementedException();
        }
    }
}
