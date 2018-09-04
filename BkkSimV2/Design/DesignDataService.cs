using System;
using BkkSimV2.Model;

namespace BkkSimV2.Design
{
    public class DesignDataService : IDataService
    {
        public void GetData(Action<DataItem, Exception> callback)
        {
            // Use this to create design time data

            var item = new DataItem("Welcome to MVVM Light [design]");
            callback(item, null);
        }

        public void GetAppConfig(Action<AppConfig, Exception> callback)
        {
            var config = new AppConfig()
            {
                Ip="127.0.0.1",
                Port=8080,
                RegistrationRefreshPeriod=10
            };
            callback(config, null);
        }

        public void GetWorkingUsers(Action<WorkingUsers, Exception> callback)
        {
            callback(new WorkingUsers()
            {
                Users = new System.Collections.Generic.List<WorkingUser>()
                {
                    new WorkingUser(){Name="User01", /*Registered=false, */Status= UserStatus.Disconnect},
                    new WorkingUser(){Name="User02", /*Registered=false, */Status= UserStatus.Disconnect},
                    new WorkingUser(){Name="User03", /*Registered=false, */Status= UserStatus.Disconnect},
                    new WorkingUser(){Name="User04", /*Registered=false, */Status= UserStatus.Disconnect},
                }
            }, null);
        }

        public void SaveWorkingUsers(Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void AddWorkingUser(string nameofuser, Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public void DelWorkingUser(string nameofuser, Action<Exception> callback)
        {
            throw new NotImplementedException();
        }

        public bool WorkingUserExist(string nameofuser)
        {
            throw new NotImplementedException();
        }
    }
}