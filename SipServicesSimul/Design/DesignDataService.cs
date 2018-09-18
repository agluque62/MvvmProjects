using System;
using SipServicesSimul.Model;

namespace SipServicesSimul.Design
{
    public class DesignDataService : IDataService
    {
        public bool AddUser(string username)
        {
            throw new NotImplementedException();
        }

        public bool DelUser(string username)
        {
            throw new NotImplementedException();
        }

        public bool UserExist(string username)
        {
            throw new NotImplementedException();
        }

        void IDataService.GetData(Action<DataConfig, Exception> callback)
        {
            DataConfig data = new DataConfig() { ListenIp = "127.0.0.0", ListenPort = 8060, LastUsers = new System.Collections.Generic.List<UserInfo>() };
            data.LastUsers.Add(new UserInfo() { Id = "345001", Status = "" });
            data.LastUsers.Add(new UserInfo() { Id = "345002", Status = "" });
            callback?.Invoke(data, null);
        }

        void IDataService.SaveData(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }
    }
}