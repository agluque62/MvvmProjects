using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WebSocketSharp;
using WebSocketSharp.Server;

using BkkSimV2.Model;
using BkkSimV2.Services;

namespace UnitsTestProject
{
    [TestClass]
    public class BkkServiceUnitTest
    {
        IDataService dataService = new DataService();
        
        [TestMethod]
        public void BkkServiceUnitTest_TestMethod1()
        {
            dataService.GetWorkingUsers((users, ex) =>
            {
                if (ex == null)
                {
                    dataService.AddWorkingUser("User-001", null);
                    dataService.AddWorkingUser("User-002", null);
                    dataService.AddWorkingUser("User-003", null);
                    dataService.AddWorkingUser("User-004", null);
                    dataService.AddWorkingUser("User-002", null);

                    dataService.DelWorkingUser("User-002", null);
                    dataService.DelWorkingUser("Userrrr-002", null);

                    dataService.SaveWorkingUsers(null);
                }
                else
                {

                }
            });
        }

        [TestMethod]
        public void BkkServiceUnitTest_TestMethod2()
        {
            var wssv = new BkkWebSocketServer(dataService, "127.0.0.1", 44444);

            wssv.Start();

            for (int i = 0; i < 10; i++)
            {
                System.Threading.Tasks.Task.Delay(2000).Wait();
                wssv.UpdateUser("User-001");
            }

            Console.ReadKey(true);

            wssv.Stop();

        }
    }
}
