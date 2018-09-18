using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace SipServicesSimul.Model
{
    public class DataService : IDataService
    {
        private const string ConfigFile = "config.json";
        private DataConfig _dataConfig = null;

        public DataService()
        {
            if (File.Exists(ConfigFile))
            {
                try
                {
                    _dataConfig = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText(ConfigFile));
                    return;
                }
                catch (Exception )
                {
                }
            }

            _dataConfig = new DataConfig() { ListenIp = "10.12.60.130", ListenPort = 8060, LastUsers = new System.Collections.Generic.List<UserInfo>() };
#if DEBUG
            _dataConfig.LastUsers.Add(new UserInfo() { Id = "345001", Status = "" });
            _dataConfig.LastUsers.Add(new UserInfo() { Id = "315002", Status = "" });
#endif
        }

        public bool AddUser(string username)
        {
            if (!UserExist(username))
            {
                _dataConfig.LastUsers.Add(new UserInfo() { Id = username, Status = "" });
                return true;
            }

            return false;
        }

        public bool DelUser(string username)
        {
            var existinguser = _dataConfig.LastUsers.Where(u => u.Id == username).FirstOrDefault();
            if (existinguser != null)
                _dataConfig.LastUsers.Remove(existinguser);
            return true;
        }

        public bool UserExist(string username)
        {
            var existinguser = _dataConfig.LastUsers.Where(u => u.Id == username).FirstOrDefault();
            return existinguser == null ? false : true;
        }

        void IDataService.GetData(Action<DataConfig, Exception> callback)
        {
            callback?.Invoke(_dataConfig, null);
        }

        void IDataService.SaveData(Action<Exception> callback)
        {
            try
            {
                File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(_dataConfig));
            }
            catch (Exception x)
            {
                callback?.Invoke(x);
            }
        }
    }
}