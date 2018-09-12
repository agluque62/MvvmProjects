using System;
using SipServicesSimul.Model;

namespace SipServicesSimul.Design
{
    public class DesignDataService : IDataService
    {
        void IDataService.GetData(Action<DataConfig, Exception> callback)
        {
            DataConfig data = new DataConfig() { ListenIp = "127.0.0.0", ListenPort = 8060, LastUsers = new System.Collections.Generic.List<UserInfo>() };
            data.LastUsers.Add(new UserInfo() { Id = "345001", Status = UserStatus.Open });
            data.LastUsers.Add(new UserInfo() { Id = "345002", Status = UserStatus.Open });
            callback?.Invoke(data, null);
        }

        void IDataService.SaveData(DataConfig dataConfig, Action<Exception> callback)
        {
            throw new NotImplementedException();
        }
    }
}