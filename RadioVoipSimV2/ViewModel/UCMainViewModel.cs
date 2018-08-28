using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RadioVoipSimV2.MvvmFramework;
using RadioVoipSimV2.Model;
using RadioVoipSimV2.Services;
using CoreSipNet;

namespace RadioVoipSimV2.ViewModel
{
    class UCMainViewModel : ViewModelBase
    {
        //private object locker = new object();

        public UCMainViewModel()
        {
            //lock (locker)
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
                    if (obj is SimulatedFrequecy)
                    {
                        var frequency = obj as SimulatedFrequecy;
                    //var receivers = frequency.Equipments.Where(eq => eq.Habilitado == true && eq.IsTx == false).ToList();
                    var receivers = AllEquipments.Where(eq => eq.FreqObject == frequency && eq.Habilitado == true && eq.IsTx == false).ToList();
                        receivers.ForEach(receiver =>
                        {
                            receiver.AircrafSquelch = !receiver.AircrafSquelch;
                            if (receiver.CallId != -1)
                            {
                                if (LocalAudioPlayer != -1 && !receiver.ScvSquelch)
                                {
                                    if (receiver.AircrafSquelch)
                                        SipAgent.MixerLink(LocalAudioPlayer, receiver.CallId);
                                    else if (!receiver.AircrafSquelch)
                                        SipAgent.MixerUnlink(LocalAudioPlayer, receiver.CallId);
                                }

                                SipAgent.SquelchSet(receiver.CallId, receiver.Squelch);
                            }
                        });
                        frequency.AircrafSquelch = !frequency.AircrafSquelch;
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
        }
        public void Dispose()
        {
            //lock (locker)
            {
                /** Cerrar el Agente SNMP*/
                SnmpAgent.Close();
                Mib.Dispose();

                /** OJO!!! Cerrar el Agente SIP */
                //if (LocalAudioPlayer != -1)
                //{
                //    SipAgent.DestroyWavPlayer(LocalAudioPlayer);
                //}

                SipAgent.End();
                LocalAudioPlayer = -1;

                /** Descargar Datos */
                Unload();
            }
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

        public List<SimulatedRadioEquipment> StandbyEquipments
        {
            get => _standbyEquipments;
            set
            {
                _standbyEquipments = value;
                OnPropertyChanged("StandbyEquipments");
            }
        }

        private AppConfig _config;
        private ControlledSipAgent _sipAgent;
        private /*ObservableCollection*/List<SimulatedFrequecy> _frecuencies;
        private List<SimulatedRadioEquipment> _standbyEquipments;
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
                Mib.NotifyReady += () =>
                {
                    /** Llevar los estados de la MIB a los estados de la aplicacion */
                    AllEquipments.ForEach((eq) =>
                    {
                        Mib.equipments.EquipmentExtendedDataGet(eq.Name, (noerror, frqId, status, data) =>
                        {
                            if (noerror)
                            {
                                eq.RemoteControlStatus = (RemoteControlStatusValues)status;
                                eq.TuneIn = frqId;
                                eq.RemoteControlExtendedData = data;
                            }
                        });
                    });                    
                };

                /** */
                Mib.NotifyExternalChange += (equipmentId) =>
                {
                    /** Cambia el estado de asignacion */
                    var eq = AllEquipments.Where(e => e.Name == equipmentId).FirstOrDefault();
                    if (eq != null)
                    {
                        Mib.equipments.EquipmentExtendedDataGet(equipmentId, (noerror, frqId, status, data) =>
                        {
                            if (noerror)
                            {
                                eq.RemoteControlStatus = (RemoteControlStatusValues)status;
                                eq.TuneIn = frqId;
                                eq.RemoteControlExtendedData = data;
                            }
                        });
                    }
                };

                Frequencies = new /*ObservableCollection*/List<SimulatedFrequecy>();
                foreach (var f in cfg.SimulatedFrequencies)
                {
                    var frequency = new SimulatedFrequecy()
                    {
                        Config = f,
                        Ptt = false,
                        Squelch = false,
                        MainEquipments = new /*ObservableCollection*/List<SimulatedRadioEquipment>(),
                        StanbyEquipments = new List<SimulatedRadioEquipment>()
                    };

                    var equipments = cfg.EquipmentsInFreq(f);
                    /** Añadir los equipos */
                    equipments.ForEach(e =>
                    {
                        var se = new SimulatedRadioEquipment(e.Id, e.Type=="Tx")
                        {
                            Config = e,
                            FreqObject = frequency,
                            TuneIn = frequency.Config.Id
                        };

                        /** Añadir el equipo a la frecuencia */
                        frequency.MainEquipments.Add(se);

                        /** Añadir el equipo a la MIB */
                        Mib.AddEquipment((table) =>
                        {
                            table.AddEquipment(
                                e.Id,
                                e.Type=="Tx" ? 1 : 0,           // transmisor
                                se.Band=="VHF" ? 0 : 1,         // vhf
                                0,                              // modo main/rsva
                                frequency.Config.Id,
                                e.ChSp,
                                e.FrOff,
                                e.Mod,
                                e.Pwr,
                                0);
                        });

                        se.NotifyRemoteControlChange += OnRemoteControlChange;
                    });
                    /** Añadir la Frecuencia */
                    Frequencies.Add(frequency);
                }

                /** Añadir los equipos en reserva */
                StandbyEquipments = new List<SimulatedRadioEquipment>();
                foreach(var e in Config.StandbyEquipments)
                {
                    var se = new SimulatedRadioEquipment(e.Id, e.Type=="Tx")
                    {
                        Config = e,
                        FreqObject = null,
                        TuneIn = "None"
                    };
                    StandbyEquipments.Add(se);

                    /** Añadirlos tambien a la MIB */
                    Mib.AddEquipment((table) =>
                    {
                        table.AddEquipment(
                            e.Id,
                            e.Type == "Tx" ? 1 : 0,          // transmisor
                            e.Band == "VHF" ? 0 : 1,         // vhf
                            1,                               // modo main/rsva
                            "",
                            0,
                            0,
                            0,
                            0,
                            0);
                    });
                    se.NotifyRemoteControlChange += OnRemoteControlChange;
                }

                /** Añadir la Referencia de los Reserva a las Frecuencias */
                Frequencies.ForEach(f =>
                {
                    f.StanbyEquipments.AddRange(StandbyEquipments);
                });

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
                //lock (locker)
                {
                    switch (ev)
                    {
                        case ControlledSipAgent.SipAgentEvents.IncomingCall:
                            ProcessIncomingCall(call, id);
                            break;
                        case ControlledSipAgent.SipAgentEvents.CallConnected:
                            ProcessCallConnected(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.CallDisconnected:
                            ProcessCallDisconnected(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.KaTimeout:
                            ProcessKATimeout(call);
                            break;
                        case ControlledSipAgent.SipAgentEvents.PttOn:
                            ProcessPtton(call, rdinfo.PttType, rdinfo.PttId, rdinfo.PttMute);
                            break;
                        case ControlledSipAgent.SipAgentEvents.PttOff:
                            ProcessPttoff(call);
                            break;
                    }
                }
            };

            SipAgent.Init(AllEquipments.Count + 5);
            SipAgent.Start();
            LocalAudioPlayer = SipAgent.CreateWavPlayer(".\\Resources\\Hold.wav", true);
        }

        private void ProcessIncomingCall(int callid, string touser)
        {
            SimulatedRadioEquipment equipment = FindEquipment(touser);
            if (equipment != null)
            {
                if (equipment.Habilitado)
                {
                    equipment.CallId = callid;
                    equipment.State = CORESIP_CallState.CORESIP_CALL_STATE_INCOMING;
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
            SimulatedRadioEquipment equipment = FindEquipment(callid);
            if (equipment != null)
            {
                equipment.State = CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED;
                /** Recuperar el sqh forzado */
                equipment.AircrafSquelch = (equipment.FreqObject != null) ? equipment.FreqObject.AircrafSquelch : equipment.AircrafSquelch;
                equipment.Squelch = true;

                SipAgent.SquelchSet(equipment.CallId, equipment.Squelch);

                if (equipment.FreqObject != null)
                    equipment.FreqObject.Status = FrequencyStatus.Operational;
            }
        }

        private void ProcessCallDisconnected(int callid)
        {
            LoggingService.From().Debug("ProcessCallDisconnected {0}", callid);
            SimulatedRadioEquipment equipment = FindEquipment(callid);
            if (equipment != null)
            {
                equipment.Reset();
                if (equipment.FreqObject != null)
                    equipment.FreqObject.Status = FrequencyStatus.NotOperational;
            LoggingService.From().Debug("CallDisconnected {0} Procesed", callid);
            }
        }

        private void ProcessKATimeout(int callid)
        {
            SimulatedRadioEquipment equipment = FindEquipment(callid);
            if (equipment != null)
            {
                SipAgent.HangupCall(callid, SipAgentNet.SIP_ERROR);
                equipment.Reset();
            }
        }

        private void ProcessPtton(int callid, CORESIP_PttType pttType, ushort pttId, int pttMute)
        {
            SimulatedRadioEquipment equipment = FindEquipment(callid);
            if (equipment != null)
            {
                if (equipment.IsTx && !equipment.Error) // Los Tx en Error no replican el PTT
                {
                    SipAgent.PttSet(callid, pttType, pttId, pttMute);
                    /** Los PTT-MUTE no replican SQH ni marcan PTT-ACTIVO */
                    if (pttMute == 0)
                    {
                        /** Replicar los SQH */
                        Task.Run(() =>
                        {
                            Task.Delay(Config.PttOn2SqhOn).Wait();
                        /** Replicacion en los equipos main */
                            List<SimulatedRadioEquipment> equipments = equipment.FreqObject.MainEquipments.Where(s => s.IsTx == false).ToList();
                        /** Replicacion en los equipos reserva */
                            equipments.AddRange(StandbyEquipments.Where(
                                stby => stby.IsTx == false &&
                                stby.FreqObject != null &&
                                stby.TuneIn == equipment.TuneIn).ToList());
                            equipments.ForEach(s =>
                            {
                                if (s.CallId != -1 && !s.Error) // Los Rx en Error no replican el SQH
                                {
                                    s.ScvSquelch = true;

                                    if (s.AircrafSquelch && LocalAudioPlayer != -1)
                                        SipAgent.MixerUnlink(LocalAudioPlayer, s.CallId);

                                    SipAgent.MixerLink(equipment.CallId, s.CallId);
                                    SipAgent.SquelchSet(s.CallId, s.Squelch, pttType, pttId);
                                }
                            });

                        });
                        equipment.Ptt = true;
                    }
                }
            }
        }

        private void ProcessPttoff(int callid)
        {
            SimulatedRadioEquipment equipment = FindEquipment(callid);
            if (equipment != null)
            {
                if (equipment.IsTx && !equipment.Error)
                {
                    SipAgent.PttSet(callid, CORESIP_PttType.CORESIP_PTT_OFF);
                    /** Replicar los SQH */
                    Task.Run(() =>
                    {
                        Task.Delay(Config.PttOff2SqhOff).Wait();
                        if (equipment.FreqObject != null)
                        {
                            /** Replicacion en los equipos main */
                            List<SimulatedRadioEquipment> equipments = equipment.FreqObject.MainEquipments.Where(s => s.IsTx == false).ToList();
                            /** Replicacion en los equipos reserva */
                            equipments.AddRange(StandbyEquipments.Where(
                                stby => stby.IsTx == false &&
                                stby.FreqObject != null &&
                                stby.TuneIn == equipment.TuneIn).ToList());

                            equipments.ForEach(s =>
                            {
                                if (s.CallId != -1)
                                {
                                    s.ScvSquelch = false;
                                    SipAgent.MixerUnlink(equipment.CallId, s.CallId);

                                    if (s.AircrafSquelch && LocalAudioPlayer != -1)
                                        SipAgent.MixerLink(LocalAudioPlayer, s.CallId);

                                    SipAgent.SquelchSet(s.CallId, s.Squelch);
                                }
                            });
                        }
                    });
                    equipment.Ptt = false;
                }
            }
        }

        private SimulatedRadioEquipment FindEquipment(string userid)
        {
            /** Busco en los equipos Main */
            var main = Frequencies.SelectMany(f => f.MainEquipments).Where(e => e.Name == userid && e.CallId == -1).FirstOrDefault();
            if (main != null)
                return main;

            /** Busco en los equipos reserva */
            var stby = StandbyEquipments.Where(e => e.Name == userid && e.CallId == -1).FirstOrDefault();            
            /** Busco la frecuencia asociada y la añado a la referencia */
            if (stby != null)
            {
                stby.FreqObject = Frequencies.Where(f => f.Config.Id == stby.TuneIn).FirstOrDefault();                
            }
            return stby;
        }

        private SimulatedRadioEquipment FindEquipment(int callid)
        {
            /** Busco en los equipos Main */
            var main = Frequencies.SelectMany(f => f.MainEquipments).Where(e => e.CallId == callid).FirstOrDefault();
            if (main != null)
                return main;

            /** Busco en los equipos reserva */
            var stby = StandbyEquipments.Where(e => e.CallId == callid).FirstOrDefault();
            /** Busco la frecuencia asociada y la añado a la referencia */
            if (stby != null)
            {
                stby.FreqObject = Frequencies.Where(f => f.Config.Id == stby.TuneIn).FirstOrDefault();
            }
            return stby;
        }

        private void SnmpAgentInitAndStart()
        {
            SnmpAgent.Init(Config.Snmp.AgentIp, null, Config.Snmp.AgentPort, 162);
            Mib.Prepare();
            SnmpAgent.Start();
        }

        private void OnRemoteControlChange(String id, RemoteControlStatusValues change)
        {
            Mib.equipments.EquipmentStatusSet(id, (int)change);
        }

        private List<SimulatedRadioEquipment> AllEquipments
        {
            get
            {
                var eqs = Frequencies.SelectMany(f => f.MainEquipments).ToList();
                eqs.AddRange(StandbyEquipments);
                return eqs;
            }
        }

    }
}
