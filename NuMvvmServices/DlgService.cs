using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NuMvvmServices
{
    public interface IDlgService
    {
        void Show(string msg, string title = "DialogService");
        void Confirm(string msg, Action<bool> SiNo, string title = "DialogService");
    }
    public class DialogService : IDlgService
    {
        public void Confirm(string msg,  Action<bool> SiNo, string title="DialogService")
        {
            MessageBoxResult result = MessageBox.Show(msg, title, MessageBoxButton.YesNo);
            SiNo?.Invoke(result == MessageBoxResult.Yes);
        }

        public void Show(string msg, string title="DialogService")
        {
            MessageBox.Show(msg, title);
        }
    }
}
