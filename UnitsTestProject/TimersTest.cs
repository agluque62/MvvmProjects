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

    internal sealed class InvalidWaitHandle : WaitHandle
    {
        private readonly static InvalidWaitHandle instance = new InvalidWaitHandle();

        private InvalidWaitHandle() { }

        public static WaitHandle Instance
        {
            get
            {
                return instance;
            }
        }

        public override IntPtr Handle
        {
            get
            {
                return WaitHandle.InvalidHandle;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }
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
                    var state = obj as TimerState;
                    Interlocked.Increment(ref state.Counter);
                    Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: starting a new callback. State={state.Counter}");
                }),
                state: timerState,
                dueTime: 1000,
                period: 100);

            while (timerState.Counter <= 100)
            {
                Task.Delay(100).Wait();
            }

            timer.Dispose(InvalidWaitHandle.Instance);
            Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: done.");

            Task.Delay(5000).Wait();
        }

        [TestMethod]
        public void TestMethod2()
        {
            timer = new Timer(
                callback: new TimerCallback((obj) =>
                {
                    Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: starting a new callback.");
                    Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                    Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: finishing callback.");
                }),
                state: null,
                dueTime: 100,
                period: Timeout.Infinite);

            Task.Delay(TimeSpan.FromSeconds(1)).Wait();

            ManualResetEvent wh = new ManualResetEvent(false);

            timer.Dispose(/*InvalidWaitHandle.Instance*/wh);
            wh.WaitOne();

            Trace.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: done.");

            //Task.Delay(TimeSpan.FromSeconds(15)).Wait();
        }

    }
}
