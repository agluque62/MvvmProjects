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
            SnmpAgentInitAndStart();

            Title = String.Format("Simulador de Equipos Radio Voip. Nucleo 2018. [{0}:{1}]", Config.VoipAgentIP, Config.VoipAgentPort);
            Btn01Text = "Config";
            Btn02Text = "Salir";

            ForceSquelchCmd = new DelegateCommandBase((obj) =>
            {
                if (obj is SimulatedRadioEquipment)
                {
                    var ses = (obj as SimulatedRadioEquipment);
                    if (ses.Habilitado == true && ses.IsTx == false)
                    {
                        ses.AircrafSquelch = !ses.AircrafSquelch;
                        if (ses.CallId != -1)
                        {
                            if (LocalAudioPlayer != -1 && !ses.ScvSquelch)
                            {
                                if (ses.AircrafSquelch)
                                    SipAgent.MixerLink(LocalAudioPlayer, ses.CallId);
                                else if (!ses.AircrafSquelch)
                                    SipAgent.MixerUnlink(LocalAudioPlayer, ses.CallId);
                            }

                            SipAgent.SquelchSet(ses.CallId, ses.Squelch);
                        }
                    }
                }
            });

            EnableDisableCmd = new DelegateCommandBase((obj) =>
            {
                if (obj is SimulatedRadioEquipment)
                {
                    var ses = (obj as SimulatedRadioEquipment);
                    if (ses.CallId != -1)
                    {
                        SipAgent.HangupCall(ses.CallId, SipAgentNet.SIP_OK);
                        ses.Reset();
                    }
                    ses.Habilitado = !ses.Habilitado;
                }
            });
        }

        public void Dispose()
        {
            /** Cerrar el Agente SNMP*/
            SnmpAgent.Close();
            Mib.Dispose();

            /** Cerrar el Agente SIP */
            if (LocalAudioPlayer != -1)
            {
                SipAgent.DestroyWavPlayer(LocalAudioPlayer);
            }
            SipAgent.End();

            /** Descargar Datos */
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
        private int LocalAudioPlayer = -1;
        private SnmpAgent.EquipmentsMib _mib;

        public SnmpAgent.EquipmentsMib Mib { get => _mib; set => _mib = value; }

        private void Load()
        {
            AppConfig.GetAppConfig((cfg, error) =>
            {
                Config = cfg;

                /** Configuracion SNMP. Creacion de la MIB */
                Mib = new SnmpAgent.EquipmentsMib(cfg.Snmp.BaseOid, /*cfg.EquipmentsCount*/11)
                {
                    QueryOid = cfg.Snmp.QueryOid,
                    AnswerOid = cfg.Snmp.AnswerOid
                };
                /** */
                Mib.NotifyExternalChange += (equipmentId) =>
                {

                };

                Frequencies = new /*ObservableCollection*/List<SimulatedFrequecy>();
                foreach (var f in cfg.SimulatedFrequencies)
                {
                    var frequency = new SimulatedFrequecy()
                    {
                        Config = f,
                        Ptt = false,
                        Squelch = false,
                        Equipments = new /*ObservableCollection*/List<SimulatedRadioEquipment>()
                    };

                    var equipments = cfg.EquipmentsInFreq(f);
                    /** Añadir los equipos */
                    equipments.ForEach(e =>
                    {
                        var se = new SimulatedRadioEquipment(e.Id, e.TxOrRx == 1)
                        {
                            Config = e,
                            FreqObject = frequency,
                            TuneIn = frequency.Config.Id
                        };

                        /** Añadir el equipo a la frecuencia */
                        frequency.Equipments.Add(se);

                        /** Añadir el equipo a la MIB */
                        Mib.AddEquipment((table) =>
                        {
                            table.AddEquipment(
                                e.Id,
                                e.TxOrRx,                       // transmisor
                                se.Band=="VHF" ? 0 : 1,         // vhf
                                0,                              // modo main/rsva
                                frequency.Config.Id,
                                e.ChSp,
                                e.FrOff,
                                e.Mod,
                                e.Pwr,
                                0);
                        });

                    });
                    /** Añadir la Frecuencia */
                    Frequencies.Add(frequency);
                }
                SelectedFreq = Frequencies.Count > 0 ? Frequencies[0] : null;
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
            LocalAudioPlayer = SipAgent.CreateWavPlayer(".\\Resources\\Hold.wav", true);
        }

        private void ProcessIncomingCall(int callid, string touser)
        {
            SimulatedFrequecy freq;
            SimulatedRadioEquipment ses;
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
            SimulatedRadioEquipment ses;
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
            SimulatedRadioEquipment ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                ses.Reset();
                freq.Status = FrequencyStatus.NotOperational;
            }
        }

        private void ProcessKATimeout(int callid)
        {
            SimulatedFrequecy freq;
            SimulatedRadioEquipment ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                SipAgent.HangupCall(callid, SipAgentNet.SIP_ERROR);
                ses.Reset();
            }
        }

        private void ProcessPtton(int callid, CORESIP_PttType pttType, ushort pttId)
        {
            SimulatedFrequecy freq;
            SimulatedRadioEquipment ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                if (ses.IsTx && !ses.Error)
                {
                    SipAgent.PttSet(callid, pttType, pttId);
                    /** Replicar los SQH */
                    Task.Run(() =>
                    {
                        Task.Delay(Config.PttOn2SqhOn).Wait();
                        ses.FreqObject.Equipments.Where(s => s.IsTx == false).ToList().ForEach(s =>
                        {
                            if (s.CallId != -1)
                            {
                                s.ScvSquelch = true;

                                if (s.AircrafSquelch && LocalAudioPlayer != -1)
                                    SipAgent.MixerUnlink(LocalAudioPlayer, s.CallId);

                                SipAgent.MixerLink(ses.CallId, s.CallId);
                                SipAgent.SquelchSet(s.CallId, s.Squelch, pttType, pttId);
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
            SimulatedRadioEquipment ses;
            if (FindFrequencyAndUser(callid, out freq, out ses))
            {
                if (ses.IsTx && !ses.Error)
                {
                    SipAgent.PttSet(callid, CORESIP_PttType.CORESIP_PTT_OFF);
                    /** Replicar los SQH */
                    Task.Run(() =>
                    {
                        Task.Delay(Config.PttOff2SqhOff).Wait();
                        ses.FreqObject.Equipments.Where(s => s.IsTx == false).ToList().ForEach(s =>
                        {
                            if (s.CallId != -1)
                            {
                                s.ScvSquelch = false;
                                SipAgent.MixerUnlink(ses.CallId, s.CallId);

                                if (s.AircrafSquelch && LocalAudioPlayer != -1)
                                    SipAgent.MixerLink(LocalAudioPlayer, s.CallId);

                                SipAgent.SquelchSet(s.CallId, s.Squelch);
                            }
                        });
                    });
                    ses.Ptt = false;
                }
            }
        }

        private bool FindFrequencyAndUser(string userid, out SimulatedFrequecy freq, out SimulatedRadioEquipment ses)
        {
            foreach (var f in Frequencies)
            {
                foreach (var u in f.Equipments)
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

        private bool FindFrequencyAndUser(int callid, out SimulatedFrequecy freq, out SimulatedRadioEquipment ses)
        {
            foreach (var f in Frequencies)
            {
                foreach (var u in f.Equipments)
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

        private void SnmpAgentInitAndStart()
        {
            SnmpAgent.Init(Config.Snmp.AgentIp, null, Config.Snmp.AgentPort, 162);
            Mib.Prepare();
            SnmpAgent.Start();
        }

    }
}
