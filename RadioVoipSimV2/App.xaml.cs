using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RadioVoipSimV2
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SetThreadPoolSize();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            RestoreThreadPoolSize();
            base.OnExit(e);
        }
        int minWorker, minIOC;
        void SetThreadPoolSize()
        {
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            if (ThreadPool.SetMinThreads(20, 20) == false)
            {
                Console.WriteLine("Error en SetMinThreads... Pulse ENTER");
                Console.ReadLine();
            }
        }
        void RestoreThreadPoolSize()
        {
            if (ThreadPool.SetMinThreads(minWorker, minIOC) == false)
            {
                Console.WriteLine("Error en SetMinThreads... Pulse ENTER");
                Console.ReadLine();
            }
        }

    }
}
