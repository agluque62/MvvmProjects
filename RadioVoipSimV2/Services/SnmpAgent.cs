using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Pipeline;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Lextm.SharpSnmpLib.Objects;

using NLog;

using Newtonsoft.Json;

namespace RadioVoipSimV2.Services
{
    public class SnmpAgent
    {
        #region CLASSES
        /// <summary>
        /// 
        /// </summary>
        public class MibObject : ScalarObject, IComparable
        {

            string _oid = "";
            public override ISnmpData Data { get; set; }

            public int CompareTo(object obj)
            {
                MibObject orderToCompare = obj as MibObject;
                return string.Compare(_oid, orderToCompare._oid);
            }

            public MibObject(string oid)
                : base(oid)
            {
                _oid = oid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        sealed class SnmpIntObject : MibObject      // ScalarObject
        {
            public int Value
            {
                get { return _data.ToInt32(); }
                set
                {
                    if (Value != value)
                    {
                        _data = new Integer32(value);

                        if ((_trapsEps != null) && (_trapsEps.Length > 0))
                        {
                            SnmpAgent.Trap(Variable.Id, _data, _trapsEps);
                        }
                    }
                }
            }

            public override ISnmpData Data
            {
                get { return _data; }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _data = (Integer32)value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="oid"></param>
            /// <param name="value"></param>
            /// <param name="trapsEps"></param>
            public SnmpIntObject(string oid, int value, params IPEndPoint[] trapsEps)
                : base(oid)
            {
                _data = new Integer32(value);
                _trapsEps = trapsEps;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="oid"></param>
            /// <returns></returns>
            public static SnmpIntObject Get(string oid)
            {
                return SnmpAgent.Store.GetObject(new ObjectIdentifier(oid)) as SnmpIntObject;
            }

            #region Private Members
            /// <summary>
            /// 
            /// </summary>
            private Integer32 _data;
            private IPEndPoint[] _trapsEps;

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        sealed class SnmpStringObject : MibObject   // ScalarObject
        {
            public string Value
            {
                get { return _data.ToString(); }
                set
                {
                    if (Value != value)
                    {
                        _data = new OctetString(value);
                        if ((_trapsEps != null) && (_trapsEps.Length > 0))
                        {
                            SnmpAgent.Trap(Variable.Id, _data, _trapsEps);
                        }
                    }
                }
            }

            public void SendTrap(string value)
            {
                OctetString data = new OctetString(value);

                if ((_trapsEps != null) && (_trapsEps.Length > 0))
                    SnmpAgent.Trap(Variable.Id, data, _trapsEps);
            }

            /// <summary>
            /// 
            /// </summary>
            public override ISnmpData Data
            {
                get { return _data; }
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    _data = (OctetString)value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="oid"></param>
            /// <param name="value"></param>
            /// <param name="trapsEps"></param>
            public SnmpStringObject(string oid, string value, params IPEndPoint[] trapsEps)
                : base(oid)
            {
                _data = new OctetString(value);
                _trapsEps = trapsEps;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="oid"></param>
            /// <returns></returns>
            public static SnmpStringObject Get(string oid)
            {
                return SnmpAgent.Store.GetObject(new ObjectIdentifier(oid)) as SnmpStringObject;
            }

            #region Private Members

            /// <summary>
            /// 
            /// </summary>
            private Lextm.SharpSnmpLib.OctetString _data;
            private IPEndPoint[] _trapsEps;

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        class SnmpLogger : Lextm.SharpSnmpLib.Pipeline.ILogger
        {
            private static readonly Logger _logger = LogManager.GetLogger("SnmpLogger");
            private const string Empty = "-";

            public SnmpLogger()
            {
            }

            public void Log(ISnmpContext context)
            {
                if (_logger.IsTraceEnabled)
                {
                    _logger.Trace(GetLogEntry(context));
                }
            }

            private static string GetLogEntry(ISnmpContext context)
            {
                return string.Format(
                     CultureInfo.InvariantCulture,
                     "{0}-{1}:{2} {3} {4}", // {5} {6} {7} {8}", // {9}",
                     context.Request.TypeCode() == SnmpType.Unknown ? Empty : context.Request.TypeCode().ToString(),
                     context.Sender.Address,
                     context.Binding.Endpoint.Port,
                     context.Request.Parameters.UserName,
                     GetStem(context.Request.Pdu().Variables),
                     // DateTime.UtcNow,
                     context.Binding.Endpoint.Address,
                     (context.Response == null) ? Empty : context.Response.Pdu().ErrorStatus.ToErrorCode().ToString(),
                     context.Request.Version,
                     DateTime.Now.Subtract(context.CreatedTime).TotalMilliseconds);
            }

            private static string GetStem(ICollection<Variable> variables)
            {
                if (variables.Count == 0)
                {
                    return Empty;
                }

                StringBuilder result = new StringBuilder();
                foreach (Variable v in variables)
                {
                    result.AppendFormat("{0};", v.Id);
                }

                if (result.Length > 0)
                {
                    result.Length--;
                }

                return result.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        class SnmpSetMessageHandler : IMessageHandler
        {
            public event Action<ISnmpContext> PduSetReceived = delegate { };

            public void Handle(ISnmpContext context, ObjectStore store)
            {
                setHandler.Handle(context, store);
                try
                {
                    PduSetReceived(context);
                }
                catch (Exception) { }
                finally
                {
                    //setHandler.Handle(context, store);
                }
            }
            SetMessageHandler setHandler = new SetMessageHandler();
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract class TableBase : TableObject
        {
            #region PUBLIC

            public String BaseOid { get => _oid_base; }
            public int RowsCount { get => _nrow; }
            public int ColCount { get => _ncol; }
            public IList<MibObject> MibObjects
            {
                get
                {
                    return _elements; ;
                }
            }

            public TableBase(string oid_base, int ncol)
            {
                _oid_base = oid_base;
                _ncol = ncol + 1;
            }
            abstract public void Tick();
            
            #endregion PUBLIC 

            #region PROTECTED

            protected MibObject ElementAt(int row, int col)
            {
                int index = row * _ncol + col + 1;
                if (index < Objects.Count())
                {
                    return MibObjects[index];
                }
                throw new Exception("SnmpAgent Exception. Elemento de Tabla fuera de rango..");
            }
            protected List<MibObject> RowContent(int row)
            {
                List<MibObject> lista = new List<MibObject>();
                for (int col = 1; col <= _ncol; col++)
                {
                    string oid_object = string.Format("{0:000}.{1:000}.{2:000}", _oid_base, col, row + 1);
                    lista.Add((SnmpAgent.Store.GetObject(new ObjectIdentifier(oid_object)) as MibObject));
                }
                return lista;
            }
            protected IList<MibObject> _elements = new List<MibObject>();
            protected string _oid_base = "";
            protected int _nrow = 0;
            protected int _ncol = 0;
            protected override IEnumerable<ScalarObject> Objects
            {
                get { return _elements; }
            }           
            protected bool FindItem(string name, int colname, int colstd, ref int que_row)
            {
                int row_libre = -1;
                for (int row = 0; row < _nrow; row++)
                {
                    string oid_name = string.Format("{0:000}.{1:000}.{2:000}", _oid_base, colname, row + 1);
                    string oid_std = string.Format("{0:000}.{1:000}.{2:000}", _oid_base, colstd, row + 1);

                    string val_name = SnmpStringObject.Get(oid_name).Value;
                    int estado = SnmpIntObject.Get(oid_std).Value;

                    if (val_name == name)
                    {
                        que_row = row + 1;
                        return true;
                    }
                    else if (row_libre == -1 && estado == -1)        // Todo. Estado Inicial debe Ser -1.
                    {
                        row_libre = row + 1;
                    }
                }

                if (row_libre != -1)
                {
                    que_row = row_libre;
                    return true;
                }
                return false;
            }
            protected void AddRow(params object[] cols)
            {
                int index = 1 + _nrow;
                int col = 1;

                _elements.Add(new SnmpIntObject(string.Format("{0:000}.{1:000}.{2:000}", _oid_base, col++, index), index));
                foreach (object obj in cols)
                {
                    if (obj is int)
                        _elements.Add(new SnmpIntObject(string.Format("{0:000}.{1:000}.{2:000}", _oid_base, col++, index), (int)obj));
                    else if (obj is string)
                        _elements.Add(new SnmpStringObject(string.Format("{0:000}.{1:000}.{2:000}", _oid_base, col++, index), (string)obj));
                    else
                        _elements.Add(new SnmpIntObject(string.Format("{0:000}.{1:000}.{2:000}", _oid_base, col++, index), (int)obj));
                }

                _nrow++;
            }
            protected int RowOf(ObjectIdentifier oid)
            {
                ObjectIdentifier Base = new ObjectIdentifier(BaseOid);
                if (oid.ToString().Contains(Base.ToString()))
                {
                    uint[] data = oid.ToNumerical();
                    return (int )data[data.Length - 1] - 1;
                }
                return -1;
            }
            
            #endregion PROTECTED
        }

        /// <summary>
        /// 
        /// </summary>
        public class EquipmentTable : TableBase
        {
            const String Filename = "last_snmp_status.json";
            class JStatusItem
            {
                public string Id { get; set; }
                public int status { get; set; }
            }

            public EquipmentTable(String baseOid, int colCount)
                : base(baseOid, colCount)
            {
            }
            public override void Tick()
            {
            }

            public string OidEquipo(string name)
            {
                ObjectIdentifier oid_base = new ObjectIdentifier(BaseOid + ".2");

                var var_equipo_name = this.MibObjects.Where(obj =>  obj.Variable.Id.ToString().StartsWith(oid_base.ToString()) &&
                   obj.Variable.Data.ToString() == name);

                if (var_equipo_name != null && var_equipo_name.Count() > 0)
                {
                    MibObject obj_equipo_name = var_equipo_name.ToList()[0];
                    string oid_equipo_name = obj_equipo_name.Variable.Id.ToString();
                    int index = oid_equipo_name.LastIndexOf(".");
                    string ret = oid_equipo_name.Substring(index, oid_equipo_name.Length - index);
                    return ret;
                }
                return "";
            }

            public void AddEquipment(string id, params object[] pars)
            {
                equipment2Index.Add(id);

                List<object> rows = new List<object>() { id };
                rows.AddRange(pars);
                AddRow(rows.ToArray());
            }

            public List<MibObject> EquipmentObjects(string id)
            {
                int row = equipment2Index.FindIndex(item => item == id);
                return row >= 0 ? RowContent(row) : null;
            }

            public string EquipmentName(string oid)
            {
                int row = RowOf(new ObjectIdentifier(oid));
                if (row != -1)
                {
                    var data = RowContent(row);
                    return (data.Count == ColCount) ? (data[1] as SnmpStringObject).Value : "Not Found";
                }
                return "Not Found";
            }

            public bool EquipmentStatusSet(String Id, int status)
            {
                List<MibObject> items = EquipmentObjects(Id);
                if (items != null && items.Count == ColCount)
                {
                    (items[10] as SnmpIntObject).Value = status;
                    return true;
                }
                return false;
            }

            public int EquipmentStatusGet(String Id)
            {
                List<MibObject> items = EquipmentObjects(Id);
                if (items != null && items.Count == ColCount)
                {
                    return (items[10] as SnmpIntObject).Value;
                }
                return 0;
            }

            public void EquipmentExtendedDataGet(string id, Action<bool, string, int, string> cb)
            {
                List<MibObject> items = EquipmentObjects(id);
                if (items != null && items.Count == ColCount)
                {
                    cb(true,                                                // Ok
                        (items[5] as SnmpStringObject).Value,               // Frecuencia
                        (items[10] as SnmpIntObject).Value,                 // Estado
                        string.Format("P:{0}, O:{1}, M:{2}, C:{3}",         // Data...
                                      (items[9] as SnmpIntObject).Value,
                                      (items[8] as SnmpIntObject).Value,
                                      (items[7] as SnmpIntObject).Value,
                                      (items[6] as SnmpIntObject).Value)
                        );
                }
                else
                {
                    cb(false, "", 0, "");
                }
            }

            public void GlobalStatusSave()
            {
                var status = (from id in equipment2Index
                              select new JStatusItem()
                              {
                                  Id = id,
                                  status = EquipmentStatusGet(id)
                              }).ToList();
                File.WriteAllText(Filename, JsonConvert.SerializeObject(status, Formatting.Indented));
            }

            public void GlobalStatusLoad()
            {
                if (!File.Exists(Filename)) return;

                var status = JsonConvert.DeserializeObject<List<JStatusItem>>(File.ReadAllText(Filename));
                status.ForEach(item =>
                {
                    EquipmentStatusSet(item.Id, item.status);
                });

            }

            private List<String> equipment2Index = new List<string>();
        }

        public class EquipmentsMib : IDisposable
        {
            public string QueryOid { get; set; }
            public string AnswerOid { get; set; }
            public string EquipmentTableOid { get; set; }

            public List<MibObject> Mib { get; set; }
            public EquipmentTable equipments { get; set; }
            public event Action<String> NotifyExternalChange = null;
            public event Action NotifyReady = null;

            public EquipmentsMib(string baseOid, int equipmentsCount)
            {
                Mib = new List<MibObject>();
                equipments = new EquipmentTable(baseOid, equipmentsCount);
            }

            public void AddEquipment(Action<EquipmentTable> callback)
            {
                callback(equipments);
            }

            public void Prepare()
            {
                /** Variables de control */
                Mib.Add(new SnmpStringObject(QueryOid, "preguntar-equipo"));
                Mib.Add(new SnmpStringObject(AnswerOid, "oid-equipo"));

                /** Variables de la tabla */
                foreach (var item in equipments.MibObjects)
                    Mib.Add(item);

                Mib.Sort();

                /** Agregarlos al almacen del agente */
                foreach (MibObject obj in Mib)
                    SnmpAgent.Store.Add(obj);

                equipments.GlobalStatusLoad();

                SnmpAgent.PduSetReceived += PduSetReceived_handler;

                NotifyReady?.Invoke();
            }

            public void GetEquipmentData(string equipmentId, Action<string, int> callback)
            {
                /** todo */
            }

            public void PduSetReceived_handler(string oid, ISnmpData data)
            {
                if (QueryOid.Contains(oid))
                {
                    /** Tengo que escribir en Properties.Settings.Default.OidRespuesta la Oid Base (fila) 
                     * de la tabla correspondiente al equipo */
                    string oid_equipo = equipments.OidEquipo(data.ToString());
                    SnmpStringObject.Get(AnswerOid).Value = oid_equipo;
                }
                else
                {
                    /** Tengo que avisar que hay cambios... */
                    NotifyExternalChange?.Invoke(equipments.EquipmentName(oid));
                }
            }

            public void Dispose()
            {
                equipments.GlobalStatusSave();
                SnmpAgent.PduSetReceived -= PduSetReceived_handler;
            }
        }


        #endregion CLASES
        /// <summary>
        /// 
        /// </summary>
        public static event Action<string, ISnmpData, IPEndPoint, IPEndPoint> TrapReceived = delegate { };
        public static event Action<string, ISnmpData> PduSetReceived = delegate { };

        public static SynchronizationContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

		public static ObjectStore Store
        {
            get { return SnmpAgent._store; }
        }

		public static void Init(string ip)
        {
            Init(ip, null, 161, 162);
        }

        static TrapV1MessageHandler trapv1 = new TrapV1MessageHandler();
        static TrapV2MessageHandler trapv2 = new TrapV2MessageHandler();
        static InformRequestMessageHandler inform = new InformRequestMessageHandler();

        static GetMessageHandler getHandler = new GetMessageHandler();
        static SnmpSetMessageHandler setHandler = new SnmpSetMessageHandler();

        public static void Init(string ip, string trapMcastIp, int port, int trap)
        {
            SnmpLogger logger = new SnmpLogger();
            ObjectStore objectStore = new ObjectStore();

            OctetString getCommunityPublic = new OctetString("public");                         // 
            OctetString setCommunityPublic = new OctetString("public");
            OctetString getCommunityPrivate = new OctetString("private");
            OctetString setCommunityPrivate = new OctetString("private");

            /** */
            trapv1 = new TrapV1MessageHandler();
            trapv2 = new TrapV2MessageHandler();
            inform = new InformRequestMessageHandler();

            IMembershipProvider[] membershipProviders = new IMembershipProvider[]
            {
				// new Version1MembershipProvider(getCommunity, setCommunity),
				new Version2MembershipProvider(getCommunityPublic, setCommunityPublic),
                new Version2MembershipProvider(getCommunityPrivate,setCommunityPublic),
                new Version3MembershipProvider()
            };
            IMembershipProvider composedMembershipProvider = new ComposedMembershipProvider(membershipProviders);

            /**/
            HandlerMapping[] handlerMappings = new HandlerMapping[]
            {
                new HandlerMapping("v1", "GET", new GetV1MessageHandler())
                ,new HandlerMapping("v2,v3", "GET", /*new GetMessageHandler()*/ getHandler)
                ,new HandlerMapping("v1", "SET", new SetV1MessageHandler())
                ,new HandlerMapping("v2,v3", "SET", /*new SetMessageHandler()*/ setHandler)
                ,new HandlerMapping("v1", "GETNEXT", new GetNextV1MessageHandler())
                ,new HandlerMapping("v2,v3", "GETNEXT", new GetNextMessageHandler())
                ,new HandlerMapping("v2,v3", "GETBULK", new GetBulkMessageHandler())
                ,new HandlerMapping("v1", "TRAPV1", trapv1)
                ,new HandlerMapping("v2,v3", "TRAPV2", trapv2)
                ,new HandlerMapping("v2,v3", "INFORM", inform)
                //,new HandlerMapping("*", "*", NullMessageHandler)
			};
            MessageHandlerFactory messageHandlerFactory = new MessageHandlerFactory(handlerMappings);

            User[] users = new User[]
            {
                new User(new OctetString("neither"), DefaultPrivacyProvider.DefaultPair),
                new User(new OctetString("authen"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("authentication")))),
                new User(new OctetString("privacy"), new DESPrivacyProvider(new OctetString("privacyphrase"), new MD5AuthenticationProvider(new OctetString("authentication"))))
            };
            UserRegistry userRegistry = new UserRegistry(users);

            EngineGroup engineGroup = new EngineGroup();
            Listener listener = new Listener() { Users = userRegistry };
            SnmpApplicationFactory factory = new SnmpApplicationFactory(logger, objectStore, composedMembershipProvider, messageHandlerFactory);

            _engine = new SnmpEngine(factory, listener, engineGroup);
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Parse(ip), port));
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Parse(ip), trap));
            _engine.ExceptionRaised += (sender, e) => _logger.Error(e.Exception, "ERROR Snmp");
            _closed = false;
            _store = objectStore;
            _context = SynchronizationContext.Current ?? new SynchronizationContext();

            trapv1.MessageReceived += TrapV1Received;
            trapv2.MessageReceived += TrapV2Received;
            inform.MessageReceived += InformRequestReceived;

            setHandler.PduSetReceived += setHandler_PduSetReceived;
        }

		public static void Start()
        {
            _engine.Start();
        }

		public static void Close()
        {
            TrapReceived = delegate { };
            PduSetReceived = delegate { };

            _closed = true;
            _engine.Stop();
            _engine.Dispose();
        }

		public static void GetValueAsync(IPEndPoint ep, string oid, Action<ISnmpData> handler)
        {
            GetValueAsync(ep, oid, handler, 2000);
        }

		public static void GetValueAsync(IPEndPoint ep, string oid, Action<ISnmpData> handler, int timeout)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<Variable> vList = new List<Variable> { new Variable(new ObjectIdentifier(oid)) };

                try
                {
                    IList<Variable> value = Messenger.Get(VersionCode.V2, ep, new OctetString("public"), vList, timeout);
                    if ((value.Count == 1) && (value[0].Data.TypeCode != SnmpType.NoSuchInstance))
                    {
                        _context.Post(delegate
                        {
                            if (!_closed)
                            {
                                handler(value[0].Data);
                            }
                        }, "SnmpAgent.ValueGetted");
                    }
                }
                catch (Exception x)
                {
                    _logger.Error("SnmpAgent::GetValueAsync", x.Message);
                    _logger.Trace(x, "SnmpAgent::GetValueAsync");
                }
            });
        }

		public static void SetValueAsync(IPEndPoint ep, string oid, ISnmpData data, Action<ISnmpData> handler)
        {
            SetValueAsync(ep, oid, data, handler, 4000);
        }

		public static void SetValueAsync(IPEndPoint ep, string oid, ISnmpData data, Action<ISnmpData> handler, int timeout)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<Variable> vList = new List<Variable> { new Variable(new ObjectIdentifier(oid), data) };

                try
                {
                    IList<Variable> value = Messenger.Set(VersionCode.V2, ep, new OctetString("private"), vList, timeout);
                    if ((value.Count == 1) && (value[0].Data.TypeCode != SnmpType.NoSuchInstance))
                    {
                        _context.Post(delegate
                        {
                            if (!_closed)
                            {
                                handler(value[0].Data);
                            }
                        }, "SnmpAgent.ValueSetted");
                    }
                }
                catch (Exception x)
                {
                    _logger.Error("SnmpAgent::SetValueAsync", x.Message);
                    _logger.Trace(x, "SnmpAgent::SetValueAsync");
                }
            });
        }

        public static void GetAsync(IPEndPoint ep, IList<Variable> vList, Action<IPEndPoint, IList<Variable>> handler)
        {
            GetAsync(ep, vList, handler, 4000);
        }

		public static void GetAsync(IPEndPoint ep, IList<Variable> vList, Action<IPEndPoint, IList<Variable>> handler, int timeout)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    IList<Variable> results = Messenger.Get(VersionCode.V2, ep, new OctetString("public"), vList, timeout);
                    _context.Post(delegate
                    {
                        if (!_closed)
                        {
                            handler(ep, results);
                        }
                    }, "SnmpAgent.GetResult");
                }
                catch (Exception x)
                {
                    _logger.Error("SnmpAgent::GetAsync", x.Message);
                    _logger.Trace(x, "SnmpAgent::GetAsync");
                }
            });
        }

		public static void Trap(string oid, ISnmpData data, params IPEndPoint[] eps)
        {
            Trap(new ObjectIdentifier(oid), data, eps);
        }

		public static void Trap(ObjectIdentifier oid, ISnmpData data, params IPEndPoint[] eps)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    List<Variable> vList = new List<Variable> { new Variable(oid, data) };

                    foreach (IPEndPoint ep in eps)
                    {
                        Messenger.SendTrapV2(0, VersionCode.V2, ep,
                            new OctetString("private"),
                            oid, // ???
                            0, vList);
                        //Messenger.SendTrapV2(0, VersionCode.V2, ep, new OctetString("private"), new ObjectIdentifier("TODO"), 0, vList);
                    }
                }
                catch (Exception x)
                {
                    _logger.Error("SnmpAgent::Trap", x.Message);
                    _logger.Trace(x, "SnmpAgent::Trap");
                }
            });
        }

        #region Private Members

        private static Logger _logger = LogManager.GetCurrentClassLogger();
		private static SnmpEngine _engine;
        private static ObjectStore _store;
        private static SynchronizationContext _context;
        private static bool _closed;

		private static void TrapV1Received(object sender, TrapV1MessageReceivedEventArgs e)
        {
            _context.Post(delegate
            {
                if (!_closed)
                {
                    var pdu = e.TrapV1Message.Pdu();
                    // if (pdu.ErrorStatus.ToInt32() == 0)
                    {
                        foreach (var v in pdu.Variables)
                        {
                            TrapReceived(v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
                        }
                    }
                }
            }, "SnmpAgent.TrapV1Received");
        }

		private static void TrapV2Received(object sender, TrapV2MessageReceivedEventArgs e)
        {
            _context.Post(delegate
            {
                if (!_closed)
                {
                    var pdu = e.TrapV2Message.Pdu();
                    //if (pdu.ErrorStatus.ToInt32() == 0)
                    {
                        foreach (var v in pdu.Variables)
                        {
                            TrapReceived(v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
                        }
                    }
                }
            }, "SnmpAgent.TrapV2Received");
        }

		private static void InformRequestReceived(object sender, InformRequestMessageReceivedEventArgs e)
        {
            _context.Post(delegate
            {
                if (!_closed)
                {
                    var pdu = e.InformRequestMessage.Pdu();
                    // if (pdu.ErrorStatus.ToInt32() == 0)
                    {
                        foreach (var v in pdu.Variables)
                        {
                            TrapReceived(v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
                        }
                    }
                }
            }, "SnmpAgent.InformReceived");
        }

        private static void setHandler_PduSetReceived(ISnmpContext context)
        {
            _context.Post(delegate
            {
                if (!_closed)
                {
                    var pdu = context.Request.Scope.Pdu;
                    {
                        foreach (var v in pdu.Variables)
                        {
                            PduSetReceived(v.Id.ToString(), v.Data);
                        }
                    }
                }
            }, "SnmpAgent.setHandler_PduSetReceived");
        }

        #endregion
    }
}
