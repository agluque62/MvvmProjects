using System;
using Uv5kiNbxSimV2.Model;

namespace Uv5kiNbxSimV2.Design
{
    public class DesignDataService : IDataService
    {
        //public void GetAppConfig(Action<AppDataConfig, Exception> callback)
        //{
        //    // Use this to connect to the actual data service
        //    var item = new AppDataConfig();
        //    callback(item, null);
        //}
        public void GetAppData(Action<AppDataConfig.JSonConfig, Exception> callback)
        {
            callback(new AppDataConfig().DesignConfig, null);
        }
        public void SetAppData(AppDataConfig.JSonConfig data)
        {
            new AppDataConfig().DesignConfig = data;
        }
    }
}