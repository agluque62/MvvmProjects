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
        void Show(string msg);
    }
    public class DialogService : IDlgService
    {
        public void Show(string msg)
        {
            MessageBox.Show(msg);
        }
    }
}
