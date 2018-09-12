using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SipServicesSimul.Model;

namespace SipServicesSimul.Services
{
    public interface ISipPresenceService
    {
        event Action<string> OptionsReceived;
        event Action<string> SubscribeReceived;
        event Action<Exception> ErrorOcurred;

        void Configure(IDataService dataService, Action<DataConfig, Exception> err);
        void Start(Action<Exception> err);
        void Stop(Action<Exception> err);

        void AddUser(string userid);
        void RemoveUser(string userid);
    }

    public class SipPresenceService : ISipPresenceService
    {
        #region ISipPresenceService

        event Action<string> ISipPresenceService.OptionsReceived
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event Action<string> ISipPresenceService.SubscribeReceived
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event Action<Exception> ISipPresenceService.ErrorOcurred
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        void ISipPresenceService.AddUser(string userid)
        {
            throw new NotImplementedException();
        }

        void ISipPresenceService.Configure(IDataService dataService, Action<DataConfig, Exception> callback)
        {
            dataService.GetData((cfg, error) =>
            {
                if (error!=null)
                {
                    callback?.Invoke(null, error);
                    return;
                }
                config = cfg;
                callback?.Invoke(cfg, null);
            });
        }

        void ISipPresenceService.RemoveUser(string userid)
        {
            throw new NotImplementedException();
        }

        void ISipPresenceService.Start(Action<Exception> err)
        {
            err?.Invoke(new NotImplementedException());
        }

        void ISipPresenceService.Stop(Action<Exception> err)
        {
            err?.Invoke(new NotImplementedException());
        }

        #endregion


        #region Private
        private DataConfig config = null;
        #endregion
    }

}
