using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BkkSimV2.Model
{
    public interface IDataService
    {
        void GetData(Action<DataItem, Exception> callback);
        void GetAppConfig(Action<AppConfig, Exception> callback);
        void GetWorkingUsers(Action<WorkingUsers, Exception> callback);
        void SaveWorkingUsers(Action<Exception> callback);
        void AddWorkingUser(string nameofuser, Action<Exception> callback);
        void DelWorkingUser(string nameofuser, Action<Exception> callback);
    }
}
