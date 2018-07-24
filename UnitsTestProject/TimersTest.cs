using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UnitsTestProject
{
    class TimerState
    {
        public int Counter;
    }

    [TestClass]
    public class TimersTest
    {
        private static Timer timer;

        [TestMethod]
        public void TestMethod1()
        {

            var timerState = new TimerState { Counter = 0 };

            timer = new Timer(
                callback: new TimerCallback((obj) => 
                {
                    Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: starting a new callback.");
                    var state = obj as TimerState;
                    Interlocked.Increment(ref state.Counter);
                }),
                state: timerState,
                dueTime: 1000,
                period: 100);

            while (timerState.Counter <= 100)
            {
                Task.Delay(100).Wait();
            }

            timer.Dispose();
            Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: done.");

            Task.Delay(5000).Wait();
        }
    }
}
