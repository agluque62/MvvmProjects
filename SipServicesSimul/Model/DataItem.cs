using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;


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

    //public enum UserStatus { Closed, Open }

    public enum ModelEvents
    {
        Message,
        Register,
        Unregister,
        StatusChange,
        SessionOpen,
        SessionClose
    }

    public class ModelEvent : EventArgs
    {
        public ModelEvent(ModelEvents ev)
        {
            Event = ev;
        }
        public ModelEvents Event { get; set; }
        public Object Data { get; set; }
    }

    public class UserInfo
    {
        public string Status { get; set; }
        public string Id { get; set; }
        public RelayCommand<string> DelUserCmd { get; set; }
        public UserInfo()
        {
            DelUserCmd = new RelayCommand<string>((user) =>
            {
                Messenger.Default.Send<ModelEvent>(new ModelEvent(ModelEvents.Unregister) { Data = this });
            });
        }
    }

    public class DataConfig
    {
        public string ListenIp { get; set; }
        public int ListenPort { get; set; }
        public List<UserInfo> LastUsers { get; set; }
    }
}