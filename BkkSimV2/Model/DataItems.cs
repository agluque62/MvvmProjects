using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BkkSimV2.Model
{
    public enum UserStatus
    {
        //Calling = 0,
        //Incoming = 1,
        //Call_Success = 2,
        //EndTalking = 12,
        //Answer_Success = 14,
        //Park_Cancel = 21,
        //Park_Start = 30,
        //StartRinging = 65,
        //Hold = 35,
        //Unhold = 36,
        //Disconnect = -1
        Unregistered = 0,
        Available = 1,
        Busy = 2,
        NotInterrupt = 3
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
        public string Name { get; set; }
        //public bool Registered { get; set; }
        public UserStatus Status { get; set; }

        public IList<UserStatus> UserStatusStrings
        {
            get
            {
                return Enum.GetValues(typeof(UserStatus)).Cast<UserStatus>().ToList<UserStatus>();
            }
        }
    }

    public class WorkingUsers
    {
        public List<WorkingUser> Users { get; set; }
    }

}