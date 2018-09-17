using System;
using System.IO;
using Newtonsoft.Json;

namespace SipServicesSimul.Model
{
    public class DataService : IDataService
    {
        private const string ConfigFile = "config.json";

        public bool AddUser(string username)
        {
            throw new NotImplementedException();
        }

        public bool DelUser(string username)
        {
            throw new NotImplementedException();
        }

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
                dataConfig.LastUsers.Add(new UserInfo() { Id = "345001", Status = "" });
                dataConfig.LastUsers.Add(new UserInfo() { Id = "315002", Status = "" });
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