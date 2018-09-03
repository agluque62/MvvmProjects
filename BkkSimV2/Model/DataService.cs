using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BkkSimV2.Model
{
    public class DataService : IDataService
    {
        private const string ConfigFile = "config.json";
        private const string WorkingUsersFile = "users.json";
        private WorkingUsers _users = null;
        private object wulocker = new object();

        public DataService()
        {
            if (File.Exists(WorkingUsersFile))
            {
                Users = JsonConvert.DeserializeObject<WorkingUsers>(File.ReadAllText(WorkingUsersFile));
            }
            else
            {
                Users = new WorkingUsers() { Users = new List<WorkingUser>() };
            }
        }

        public void GetData(Action<DataItem, Exception> callback)
        {
            // Use this to connect to the actual data service

            var item = new DataItem("Welcome to MVVM Light");
            callback?.Invoke(item, null);
        }

        public void GetAppConfig(Action<AppConfig, Exception> callback)
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(ConfigFile));
                    callback?.Invoke(config, null);
                }
                else
                {
                    callback?.Invoke(new AppConfig() { Ip = "127.0.0.1", Port=8080, RegistrationRefreshPeriod=60 }, null);
                }
            }
            catch (Exception x)
            {
                callback?.Invoke(null, x);
            }
        }

        public void GetWorkingUsers(Action<WorkingUsers, Exception> callback)
        {
            try
            {
                lock (wulocker)
                {
                    callback?.Invoke(Users, null);
                }
            }
            catch (Exception x)
            {
                callback?.Invoke(null, x);
            }
        }

        public void SaveWorkingUsers(Action<Exception> callback)
        {
            try
            {
                lock (wulocker)
                {
                    if (Users != null)
                    {
                        File.WriteAllText(WorkingUsersFile, JsonConvert.SerializeObject(Users));
                    }
                }
            }
            catch (Exception x)
            {
                callback?.Invoke(x);
            }
        }

        public void AddWorkingUser(string nameofuser, Action<Exception> callback)
        {
            lock (wulocker)
            {
                var exist = Users?.Users?.Any(u => u.Name == nameofuser);
                if (exist!=null && exist==false)
                {
                    Users?.Users.Add(new WorkingUser() { Name = nameofuser, /*Registered = false, */Status = UserStatus.Available });
                }
            }
        }

        public void DelWorkingUser(string nameofuser, Action<Exception> callback)
        {
            lock (wulocker)
            {
                var user = Users?.Users?.Find(u => u.Name == nameofuser);
                if (user != null)
                {
                    Users?.Users.Remove(user);
                }
            }
        }

        public bool WorkingUserExist(string nameofuser)
        {
            lock (wulocker)
            {
                var exist = Users?.Users?.Any(u => u.Name == nameofuser);
                return exist==null ? false : (bool)exist;
            }
            throw new NotImplementedException();
        }

        private WorkingUsers Users { get => _users; set => _users = value; }

    }

}