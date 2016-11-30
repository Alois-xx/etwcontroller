using ETWController;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    [TestFixture]
    class TraceControlerServiceTests
    {
        [Test]
        public void Can_Execute_WPR()
        {
            TraceControlerService service = new TraceControlerService();
            Tuple<int,string> lret = service.ExecuteWPRCommand("-status");
            Assert.IsTrue(lret.Item2.Contains("WPR is not recording"));
            Assert.AreEqual(0, lret.Item1);
        }


        [Test]
        public void ListActiveSessions()
        {
            TraceControlerService service = new TraceControlerService();
            foreach (var name in service.GetTraceSessions())
            {
                Console.WriteLine(name); 
            }
        }
    }
}
