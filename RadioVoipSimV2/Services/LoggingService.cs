using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace RadioVoipSimV2.Services
{
    class LoggingService
    {
        public static LoggingService From([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, [System.Runtime.CompilerServices.CallerMemberName] string caller = null)
        {
            return new LoggingService(String.Format("[{0};{1}]: ", caller, lineNumber));
        }

        public LoggingService(string from)
        {
            FromStr = from;
        }

        public void Debug(string msg, params object[] par)
        {
            Log.Debug(FromStr + msg, par);
        }

        protected string FromStr { get; set; }
        protected Logger Log = LogManager.GetLogger("LogService");
    }
}
