using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using RadioVoipSimV2.Model;
using CoreSipNet;

namespace RadioVoipSimV2.ViewModel
{
    class UCMainViewModel : ViewModelBase
    {

        public UCMainViewModel()
        {
            _uiContext = System.Threading.SynchronizationContext.Current;

            Load();
            SipAgentInitAndStart();

            Title = String.Format("Simulador de Equipos Radio Voip. Nucleo 2018. [{0}:{1}]", Config.VoipAgentIP, Config.VoipAgentPort);
        }

        public void Dispose()
        {
            SipAgent.End();
            Unload();
        }

        public DelegateCommandBase ForceSquelchCmd { get => _forceSquelchCmd; set => _forceSquelchCmd = value; }
        public DelegateCommandBase EnableDisableCmd { get => _enableDisableCmd; set => _enableDisableCmd = value; }

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
        public /*ObservableCollection*/List<SimulatedFrequecy> Frequencies
        {
            get => _frecuencies;
            set
            {
                _frecuencies = value;
                OnPropertyChanged("Frequencies");
            }
        }
        public SimulatedFrequecy SelectedFreq
        {
            get => _selectedFreq;
            set
            {
                _selectedFreq = value;
                OnPropertyChanged("SelectedFreq");
            }
        }

        private AppConfig _config;
        private ControlledSipAgent _sipAgent;
        private /*ObservableCollection*/List<SimulatedFrequecy> _frecuencies;
        private SimulatedFrequecy _selectedFreq;
        private System.Threading.SynchronizationContext _uiContext = null;
        private DelegateCommandBase _forceSquelchCmd;
        private DelegateCommandBase _enableDisableCmd;

        private void Load()
        {
            AppConfig.GetAppConfig((cfg, error) =>
            {
                Config = cfg;

                Frequencies = new /*ObservableCollection*/List<SimulatedFrequecy>();
                foreach (var f in cfg.SimulatedFrequencies)
                {
                    var frequency = new SimulatedFrequecy()
                    {
                        Config = f,
                        Ptt = false,
                        Squelch = false,
                        Sessions = new /*ObservableCollection*/List<SipSession>()
                    };
                    /** Añadir los Usuarios de tx */
                    f.TxUsers.ForEach(tx =>
                    {
                        frequency.Sessions.Add(new SipSession(tx, true) { Freq = frequency });
                    });
                    /** Añadir los usuarios de rx */
                    f.RxUsers.ForEach(rx =>
                    {
                        frequency.Sessions.Add(new SipSession(rx) { Freq = frequency });
                    });

                    Frequencies.Add(frequency);
                }
                SelectedFreq = Frequencies.Count > 0 ? Frequencies[0] : null;
            });

            ForceSquelchCmd = new DelegateCommandBase((obj) =>
            {
                if (obj is SipSession)
                {
                    var ses = (obj as SipSession);
                    if (ses.Habilitado==true && ses.IsTx == false)
                    {
                        if (ses.CallId != -1)
                        {
                            ses.AircrafSquelch = !ses.AircrafSquelch;
                            SipAgent.SquelchSet(ses.CallId, ses.Squelch);
                        }
                    }
                }
            });

            EnableDisableCmd = new DelegateCommandBase((obj)=>{
                if (obj is SipSession)
                {
                    var ses = (obj as SipSession);
                    if (ses.CallId != -1)
                    {
                        SipAgent.HangupCall(ses.CallId, SipAgentNet.SIP_OK);
                        ses.Reset();
                    }
                    ses.Habilitado = !ses.Habilitado;
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
            SipAgent.SipAgentEvent += (ev, call, id, rdinfo) =>
            {
                /** Ejecutar los eventos en el contexto UI */
                //_uiContext.Send(x =>
                //{
                    switch (ev)
                    {
                        case ControlledSipAgent.SipAgentEvents.IncomingCall:
                            ProcessIncomingCall(call, id);
                            break;
                        case ControlledSipAgent.SipAgentEvents.CallConnected:
                            ProcessCallConnected(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.CallDisconnected:
                            ProcessCallConnected(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.KaTimeout:
                            ProcessKATimeout(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.PttOn:
                            ProcessPtton(call, rdinfo.PttType, rdinfo.PttId);
                            break;
                        case ControlledSipAgent.SipAgentEvents.PttOff:
                            ProcessPttoff(call);
                            break;
                    }
                //}, null);
            };

            SipAgent.Init();
            SipAgent.Start();
        }

        private void ProcessIncomingCall(int callid, string touser)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(touser, out freq, out ses))
            {
                if (ses.Habilitado)
                {
                    ses.CallId = callid;
                    ses.State = CORESIP_CallState.CORESIP_CALL_STATE_INCOMING;
                    SipAgent.AnswerCall(callid, SipAgentNet.SIP_OK);
                }
                else
                {
                    SipAgent.HangupCall(callid, SipAgentNet.SIP_TEMPORARILY_UNAVAILABLE);
                }
                return;
            }
            SipAgent.HangupCall(callid, SipAgentNet.SIP_NOT_FOUND);
        }

        private void ProcessCallConnected(int callid)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                ses.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
                /** Recuperar el sqh forzado */
                ses.Squelch = true;
                SipAgent.SquelchSet(ses.CallId, ses.Squelch);

                freq.Status = FrequencyStatus.Operational;
            }
        }

        private void ProcessCallDisconnected(int callid)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                ses.Reset();
                freq.Status = FrequencyStatus.NotOperational;
            }
        }

        private void ProcessKATimeout(int callid)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                SipAgent.HangupCall(callid, SipAgentNet.SIP_ERROR);
                ses.Reset();
            }
        }

        private void ProcessPtton(int callid, CORESIP_PttType pttType, int pttId)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                if (ses.IsTx && !ses.Error)
                {
                    SipAgent.PttSet(callid, pttType, (ushort)pttId);
                    /** Replicar los SQH */
                    Task.Run(() =>
                    {
                        Task.Delay(Config.PttOn2SqhOn).Wait();
                        ses.Freq.Sessions.Where(s => s.IsTx == false).ToList().ForEach(s =>
                        {
                            if (s.CallId != -1)
                            {
                                s.ScvSquelch = true;
                                SipAgent.SquelchSet(s.CallId, s.Squelch);
                            }
                        });

                    });
                    ses.Ptt = true;
                }
            }
        }

        private void ProcessPttoff(int callid)
        {
            SimulatedFrequecy freq;
            SipSession ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                if (ses.IsTx && !ses.Error)
                {
                    SipAgent.PttSet(callid, CORESIP_PttType.CORESIP_PTT_OFF);
                    /** Replicar los SQH */
                    Task.Run(() =>
                    {
                        Task.Delay(Config.PttOff2SqhOff).Wait();
                        ses.Freq.Sessions.Where(s => s.IsTx == false).ToList().ForEach(s =>
                        {
                            if (s.CallId != -1)
                            {
                                s.ScvSquelch = false;
                                SipAgent.SquelchSet(s.CallId, s.Squelch);
                            }
                        });
                    });
                    ses.Ptt = false;
                }
            }
        }

        private bool FindFrequencyAndUser(string userid, out SimulatedFrequecy freq, out SipSession ses)
        {
            foreach (var f in Frequencies)
            {
                foreach (var u in f.Sessions)
                {
                    if (u.Name == userid && u.CallId == -1)
                    {
                        freq = f;
                        ses = u;
                        return true;
                    }
                }
            }
            freq = null; ses = null;
            return false;
        }

        private bool FindFrequencyAndUser(int callid, out SimulatedFrequecy freq, out SipSession ses)
        {
            foreach (var f in Frequencies)
            {
                foreach (var u in f.Sessions)
                {
                    if (u.CallId == callid)
                    {
                        freq = f;
                        ses = u;
                        return true;
                    }
                }
            }
            freq = null; ses = null;
            return false;
        }

        private SimulatedFrequecy FindFrequency(string userid)
        {
            return null;
        }

    }
}
