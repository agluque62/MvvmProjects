using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NuMvvmServices;

namespace UnitsTestProject
{
    [TestClass]
    public class NuMvvmServicesTests
    {
        [TestMethod]
        public void DialogServiceTest1()
        {
            IDlgService srv = new DialogService();

            srv.Confirm("Prueba de Confirmacion", (res) =>
            {
                srv.Show($"Resultado: {res}");
            });
        }
    }
}
