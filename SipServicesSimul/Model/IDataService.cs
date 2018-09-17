using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SipServicesSimul.Model
{
    public interface IDataService
    {
        void GetData(Action<DataConfig, Exception> callback);
        void SaveData(DataConfig dataConfig, Action<Exception> callback);

        bool AddUser(string username);
        bool DelUser(string username);
    }
}
