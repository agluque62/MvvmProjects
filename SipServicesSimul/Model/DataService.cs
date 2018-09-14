using System;
using System.IO;
using Newtonsoft.Json;

namespace SipServicesSimul.Model
{
    public class DataService : IDataService
    {
        private const string ConfigFile = "config.json";

        void IDataService.GetData(Action<DataConfig, Exception> callback)
        {
            DataConfig dataConfig;
            if (File.Exists(ConfigFile))
            {
                try
                {
                    dataConfig = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText(ConfigFile));
                }
                catch(Exception x)
                {
                    callback?.Invoke(null, x);
                    return;
                }
            }
            else
            {
                dataConfig = new DataConfig() { ListenIp = "10.12.60.130", ListenPort = 8060, LastUsers = new System.Collections.Generic.List<UserInfo>() };
                dataConfig.LastUsers.Add(new UserInfo() { Id = "345001", Status = UserStatus.Open });
                dataConfig.LastUsers.Add(new UserInfo() { Id = "345002", Status = UserStatus.Open });
            }
            callback?.Invoke(dataConfig, null);
        }

        void IDataService.SaveData(DataConfig dataConfig, Action<Exception> callback)
        {
            try
            {
                File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(dataConfig));
            }
            catch (Exception x)
            {
                callback?.Invoke(x);
            }
        }
    }
}