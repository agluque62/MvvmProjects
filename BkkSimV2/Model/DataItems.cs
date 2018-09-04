using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;

using Newtonsoft.Json;

namespace BkkSimV2.Model
{
    public enum ModelEvents
    {
        Message,
        Register,
        Unregister,
        StatusChange,
        SessionOpen,
        SessionClose
    }

    public enum UserStatus
    {
        Calling = 0,
        Incoming = 1,
        Call_Success = 2,
        EndTalking = 12,
        Answer_Success = 14,
        Park_Cancel = 21,
        Park_Start = 30,
        StartRinging = 65,
        Hold = 35,
        Unhold = 36,
        Disconnect = -1,
        //Unregistered = -2,
        //Available = 0, 
        //Busy = 2,
        //NotInterrupt = 3
    }

    public class DataItem
    {
        public string Title
        {
            get;
            private set;
        }

        public DataItem(string title)
        {
            Title = title;
        }
    }

    public class AppConfig
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public int RegistrationRefreshPeriod { get; set; }
    }

    public class WorkingUser
    {
        private UserStatus _status;


        public WorkingUser()
        {
            AppDelUser = new RelayCommand<string>((obj) =>
            {
                Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.Unregister) { Data = this });
            });
        }

        public string Name { get; set; }
        public UserStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ModelEvents.StatusChange) { Data = this });
            }
        }

        [JsonIgnore]
        public IList<UserStatus> UserStatusStrings
        {
            get
            {
                return Enum.GetValues(typeof(UserStatus)).Cast<UserStatus>().ToList<UserStatus>();
            }
        }

        [JsonIgnore]
        public RelayCommand<string> AppDelUser { get; set; }
    }

    public class WorkingUsers
    {
        public List<WorkingUser> Users { get; set; }
    }

    public class BkkSimEvent : EventArgs
    {
        public BkkSimEvent(ModelEvents ev)
        {
            Event = ev;
        }
        public ModelEvents Event { get; set; }
        public Object Data { get; set; }
    }

    public class BkkMessaging
    {
        public static void Send(ModelEvents ev, Object data)
        {
            Messenger.Default.Send<BkkSimEvent>(new BkkSimEvent(ev) { Data = data });
        }
    }
}