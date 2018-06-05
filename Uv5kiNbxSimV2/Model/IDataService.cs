using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uv5kiNbxSimV2.Model
{
    public interface IDataService
    {
        // void GetAppConfig(Action<AppDataConfig, Exception> callback);
        void GetAppData(Action<AppDataConfig.JSonConfig, Exception> callback);
        void SetAppData(AppDataConfig.JSonConfig data);
    }
}
