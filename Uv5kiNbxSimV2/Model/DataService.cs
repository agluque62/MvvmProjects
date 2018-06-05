using System;

namespace Uv5kiNbxSimV2.Model
{
    public class DataService : IDataService
    {

        //public void GetAppConfig(Action<AppDataConfig, Exception> callback)
        //{
        //    var item = new AppDataConfig();
        //    callback(item, null);
        //}

        public void GetAppData(Action<AppDataConfig.JSonConfig, Exception> callback)
        {
            callback(new AppDataConfig().Config, null);
        }

        public void SetAppData(AppDataConfig.JSonConfig data)
        {
            new AppDataConfig().Config = data;
        }        
    }
}