using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
namespace RadioVoipSimV2.ViewModel
{
    class UCMainViewModel : ViewModelBase
    {
        public UCMainViewModel()
        {
            CmdEjemplo = new DelegateCommandBase((parametros) =>
            {
                System.Windows.MessageBox.Show("HolaHola");
            });
        }

        public DelegateCommandBase CmdEjemplo { get; set; }
    }
}
