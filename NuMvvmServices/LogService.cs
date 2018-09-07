using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using NLog;

namespace NuMvvmServices
{
    public interface ILogService
    {
        ILogService From([CallerFilePathAttribute ] string caller = null, [CallerLineNumber] int lineNumber = 0);

        void Trace(string msg, params object[] par);
        void Debug(string msg, params object[] par);
        void Info(string msg, params object[] par);
        void Warn(string msg, params object[] par);
        void Error(string msg, params object[] par);
        void Fatal(string msg, params object[] par);

        void TraceException(Exception x);
    }
    public class LogService : ILogService
    {
        #region private / protected
            protected Logger Log = LogManager.GetLogger("LogService");
        #endregion

        ILogService ILogService.From(string caller, int lineNumber)
        {
            caller = System.IO.Path.GetFileName(caller);
            LogManager.Configuration.Variables["file"] = caller;
            LogManager.Configuration.Variables["line"] = lineNumber.ToString();
            return this;
        }

        void ILogService.Trace(string msg, params object[] par)
        {
            Log.Trace(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.Debug(string msg, params object[] par)
        {
            Log.Debug(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.Info(string msg, params object[] par)
        {
            Log.Info(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.Warn(string msg, params object[] par)
        {
            Log.Warn(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.Error(string msg, params object[] par)
        {
            Log.Error(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.Fatal(string msg, params object[] par)
        {
            Log.Fatal(msg.Replace(System.Environment.NewLine, "--"), par);
        }

        void ILogService.TraceException(Exception x)
        {
            Log.Log<Exception>(LogLevel.Trace, x);
        }
    }
}
