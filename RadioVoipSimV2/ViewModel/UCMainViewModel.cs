using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using RadioVoipSimV2.Model;

namespace RadioVoipSimV2.ViewModel
{
    class UCMainViewModel : ViewModelBase
    {
        private DelegateCommandBase _cmdEjemplo;
        private AppConfig _config;
        private ControlledSipAgent _sipAgent;
        private ObservableCollection<SimulatedFrequecy> _frecuencies;
        private System.Threading.SynchronizationContext _uiContext = null;

        public UCMainViewModel()
        {
            _uiContext = System.Threading.SynchronizationContext.Current;

            Load();
            SipAgentInitAndStart();

            Title = String.Format("Simulador de Equipos Radio Voip. Nucleo 2018. [{0}:{1}]", Config.VoipAgentIP, Config.VoipAgentPort);

            Task.Factory.StartNew(() =>
            {
                Task.Delay(2000).Wait();
                _uiContext.Send(x =>
                {
                    Frequencies[0].Ptt = true;
                    Frequencies[1].Squelch = true;
                    OnPropertyChanged("Ptt");
                },null);
            });

            CmdEjemplo = new DelegateCommandBase((parametros) =>
            {
                System.Windows.MessageBox.Show("HolaHola");
            });
        }

        public void Dispose()
        {
            SipAgent.End();
            Unload();
        }

        public DelegateCommandBase CmdEjemplo { get => _cmdEjemplo; set => _cmdEjemplo = value; }
        public AppConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged("Config");
            }
        }
        public ControlledSipAgent SipAgent { get => _sipAgent; set => _sipAgent = value; }
        public ObservableCollection<SimulatedFrequecy> Frequencies
        {
            get => _frecuencies;
            set
            {
                _frecuencies = value;
                OnPropertyChanged("Frequencies");
            }
        }

        private void Load()
        {
            AppConfig.GetAppConfig((cfg, error) =>
            {
                Config = cfg;

                Frequencies = new ObservableCollection<SimulatedFrequecy>();
                foreach (var f in cfg.SimulatedFrequencies)
                {
                    Frequencies.Add(new SimulatedFrequecy()
                    {
                        Config = f,
                        Ptt = false,
                        Squelch = false,
                        Sessions = new ObservableCollection<SipSesion>()
                    });
                }
            });

        }
        private void Unload()
        {
            Frequencies.Clear();
        }

        private void SipAgentInitAndStart()
        {
            SipAgent = new ControlledSipAgent() { IpBase = Config.VoipAgentIP, SipPort = (uint)Config.VoipAgentPort, CoresipLogLevel = 3 };
            SipAgent.SipAgentEvent += (ev, call, id) =>
            {
                switch (ev)
                {
                    case ControlledSipAgent.SipAgentEvents.IncomingCall:
                        break;
                    case ControlledSipAgent.SipAgentEvents.CallConected:
                        break;
                    case ControlledSipAgent.SipAgentEvents.CallDisconected:
                        break;
                    case ControlledSipAgent.SipAgentEvents.KaTimeout:
                        break;
                    case ControlledSipAgent.SipAgentEvents.PttOn:
                        break;
                    case ControlledSipAgent.SipAgentEvents.PttOff:
                        break;
                }
            };
            SipAgent.Init();
            SipAgent.Start();
        }
    }
}
