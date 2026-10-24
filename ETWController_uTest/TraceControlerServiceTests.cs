using ETWController;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWController_uTest
{
    public class TraceControlerServiceTests
    {
        [Fact]
        public void Can_Execute_WPR()
        {
            TraceControlerService service = new TraceControlerService();
            Tuple<int,string> lret = service.ExecuteWPRCommand("-status");
            Assert.Contains("WPR is not recording", lret.Item2);
            Assert.Equal(0, lret.Item1);
        }


        [Fact]
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
