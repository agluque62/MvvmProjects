using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SipServicesSimul.Model
{
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

    public enum UserStatus { Closed, Open }

    public class UserInfo
    {
        public UserStatus Status { get; set; }
        public string Id { get; set; }
    }

    public class DataConfig
    {
        public string ListenIp { get; set; }
        public int ListenPort { get; set; }
        public List<UserInfo> LastUsers { get; set; }
    }
}